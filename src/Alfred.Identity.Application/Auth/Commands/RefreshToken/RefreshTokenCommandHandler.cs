using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Common.Enums;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand - implements token rotation
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILocationService _locationService;
    private readonly ICacheProvider _cacheProvider;

    private const int RefreshTokenLifetimeSeconds = 604800; // 7 days

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        ITokenRepository tokenRepository,
        IJwtTokenService jwtTokenService,
        ILocationService locationService,
        ICacheProvider cacheProvider)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _jwtTokenService = jwtTokenService;
        _locationService = locationService;
        _cacheProvider = cacheProvider;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Hash the incoming refresh token
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);

        // Find the refresh token
        var storedToken = await _tokenRepository.GetByReferenceIdAsync(tokenHash, cancellationToken);
        if (storedToken == null)
        {
            return new RefreshTokenResult(false, Error: "Invalid refresh token");
        }

        // Check if token is already used (potential token reuse attack)
        if (storedToken.Status == TokenStatus.Redeemed || storedToken.RedemptionDate.HasValue)
        {
            // Revoke all tokens for this user (security measure)
            if (storedToken.UserId.HasValue)
            {
                await _tokenRepository.RevokeAllByUserIdAsync(storedToken.UserId.Value, cancellationToken);
                await _tokenRepository.SaveChangesAsync(cancellationToken);
            }

            return new RefreshTokenResult(false, Error: "Token has been reused - all sessions revoked");
        }

        // Check if token is revoked or expired
        if (storedToken.Status != TokenStatus.Valid ||
            (storedToken.ExpirationDate.HasValue && DateTime.UtcNow > storedToken.ExpirationDate.Value))
        {
            return new RefreshTokenResult(false, Error: "Refresh token is invalid or expired");
        }

        // Check if session was explicitly revoked via Redis (immediate effect from session management)
        if (await _cacheProvider.ExistsAsync($"session:revoked:{storedToken.Id}", cancellationToken))
        {
            return new RefreshTokenResult(false, Error: "Session has been revoked");
        }

        // Also check session-level revocation (set by RevokeSessionCommandHandler)
        if (storedToken.AuthorizationId.HasValue &&
            await _cacheProvider.ExistsAsync($"revoked:session:{storedToken.AuthorizationId.Value}", cancellationToken))
        {
            return new RefreshTokenResult(false, Error: "Session has been revoked");
        }

        // Get user
        if (!storedToken.UserId.HasValue)
        {
            return new RefreshTokenResult(false, Error: "Invalid token state");
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId.Value, cancellationToken);
        if (user == null || !user.CanLogin())
        {
            return new RefreshTokenResult(false, Error: "User account is not active");
        }

        // Mark old token as used via direct SQL UPDATE (bypasses change tracking).
        // RedeemByIdAsync uses ExecuteUpdateAsync with WHERE status=Valid, so it is
        // idempotent: 0 rows if already redeemed/deleted by a concurrent request — no exception.
        await _tokenRepository.RedeemByIdAsync(storedToken.Id, cancellationToken);

        // Generate new tokens
        var accessToken =
            await _jwtTokenService.GenerateAccessTokenAsync(user.Id.Value, user.Email, user.FullName,
                storedToken.ApplicationId?.Value, storedToken.AuthorizationId?.Value);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var jwtId = _jwtTokenService.GetJwtIdFromToken(accessToken);

        // Get location from IP
        var location = request.IpAddress != null
            ? await _locationService.GetLocationFromIpAsync(request.IpAddress)
            : null;

        // Create and store new refresh token
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshTokenValue);

        var properties = location != null
            ? $"{{\"location\": \"{location}\", \"device\": \"{request.DeviceName ?? "Unknown"}\", \"ip\": \"{request.IpAddress}\"}}"
            : null;

        var newRefreshToken = Token.Create(
            OAuthConstants.TokenTypes.RefreshToken,
            storedToken.ApplicationId,
            user.Id.ToString(),
            user.Id,
            DateTime.UtcNow.AddSeconds(RefreshTokenLifetimeSeconds),
            newRefreshTokenHash,
            storedToken.AuthorizationId,
            null,
            properties,
            request.IpAddress,
            location,
            request.DeviceName
        );

        await _tokenRepository.AddAsync(newRefreshToken, cancellationToken);

        // SaveChangesAsync now only handles the new RT INSERT.
        // RedeemByIdAsync above used ExecuteUpdateAsync which auto-commits independently.
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        // Cleanup stale tokens. Must be awaited — fire-and-forget on a scoped DbContext causes
        // Npgsql to receive BindComplete while the connection is already being closed on scope dispose.
        try
        {
            await _tokenRepository.DeleteExpiredAndRedeemedByUserAsync(user.Id, CancellationToken.None);
        }
        catch
        {
            /* non-critical cleanup — swallow */
        }

        return new RefreshTokenResult(
            true,
            accessToken,
            newRefreshTokenValue,
            900 // 15 minutes
        );
    }
}
