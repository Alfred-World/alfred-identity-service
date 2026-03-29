using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Common.Enums;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.RevokeSession;

public class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, Result<object>>
{
    private readonly ITokenRepository _tokenRepository;
    private readonly ICacheProvider _cacheProvider;
    private readonly IJwtTokenService _jwtTokenService;

    public RevokeSessionCommandHandler(
        ITokenRepository tokenRepository,
        ICacheProvider cacheProvider,
        IJwtTokenService jwtTokenService)
    {
        _tokenRepository = tokenRepository;
        _cacheProvider = cacheProvider;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<object>> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        var token = await _tokenRepository.GetByIdAsync(new TokenId(request.TokenId), cancellationToken);

        if (token == null)
        {
            return Result<object>.Failure("SessionNotFound");
        }

        // Ensure the token belongs to this user
        if (token.UserId != new UserId(request.UserId))
        {
            return Result<object>.Failure("SessionNotFound");
        }

        if (token.Status == TokenStatus.Revoked)
        {
            return Result<object>.Success(new { });
        }

        // Revoke ALL tokens under this authorization session (RT + any duplicates)
        if (token.AuthorizationId.HasValue)
        {
            await _tokenRepository.RevokeAllByAuthorizationIdAsync(token.AuthorizationId.Value, cancellationToken);
        }
        else
        {
            token.Revoke();
            _tokenRepository.Update(token);
        }

        await _tokenRepository.SaveChangesAsync(cancellationToken);

        // ── Redis JTI / Session blocklist ───────────────────────────────────────────
        // Strategy: blocklist at the session (authorization) level rather than per-JTI,
        // because we never store individual AT JTIs in the DB.
        //
        // The AT carries an "authorization_id" claim. The Gateway middleware reads this
        // claim and checks "revoked:session:{authorizationId}" in Redis.
        // TTL = AT lifetime: once all ATs from this session have naturally expired,
        // the Redis key is no longer needed.
        if (token.AuthorizationId.HasValue)
        {
            var sessionKey = $"revoked:session:{token.AuthorizationId.Value}";
            var atTtl = TimeSpan.FromSeconds(_jwtTokenService.AccessTokenLifetimeSeconds);
            await _cacheProvider.SetAsync(sessionKey, "1", atTtl, cancellationToken);
        }

        // Legacy per-token key kept for backwards compatibility with any existing checks
        var legacyKey = $"session:revoked:{token.Id}";
        var legacyTtl = token.ExpirationDate.HasValue
            ? token.ExpirationDate.Value - DateTime.UtcNow
            : TimeSpan.FromDays(14);

        if (legacyTtl > TimeSpan.Zero)
        {
            await _cacheProvider.SetAsync(legacyKey, "1", legacyTtl, cancellationToken);
        }

        return Result<object>.Success(new { });
    }
}
