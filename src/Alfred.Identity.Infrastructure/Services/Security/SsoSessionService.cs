using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Common.Enums;
using Alfred.Identity.Domain.Entities;

using Microsoft.IdentityModel.Tokens;

namespace Alfred.Identity.Infrastructure.Services.Security;

public sealed class SsoSessionService : ISsoSessionService
{
    private const string ActiveCacheValue = "active";
    private const string RevokedCacheValue = "revoked";
    private static readonly TimeSpan RevokedFallbackTtl = TimeSpan.FromDays(1);

    private readonly ITokenRepository _tokenRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICacheProvider _cacheProvider;

    public SsoSessionService(
        ITokenRepository tokenRepository,
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        ICacheProvider cacheProvider)
    {
        _tokenRepository = tokenRepository;
        _applicationRepository = applicationRepository;
        _userRepository = userRepository;
        _cacheProvider = cacheProvider;
    }

    public string SsoSessionClaimType => "sso_sid";

    public async Task<CreateSsoSessionResult> CreateAsync(
        UserId userId,
        bool rememberMe,
        DateTimeOffset expiresUtc,
        string? ipAddress,
        string? device,
        CancellationToken cancellationToken = default)
    {
        var ssoApp = await _applicationRepository.GetByClientIdAsync(ApplicationConstants.ClientIds.SsoWeb,
            cancellationToken);
        if (ssoApp == null)
        {
            throw new InvalidOperationException("SSO application is not configured.");
        }

        var sessionId = GenerateSessionId();
        var sessionHash = HashSessionId(sessionId);
        var expirationDate = expiresUtc.UtcDateTime;
        var properties = JsonSerializer.Serialize(new { remember_me = rememberMe });

        var token = Token.Create(
            OAuthConstants.TokenTypes.SsoSession,
            ssoApp.Id,
            userId.ToString(),
            userId,
            expirationDate,
            sessionHash,
            properties: properties,
            ipAddress: ipAddress,
            device: device);

        await _tokenRepository.AddAsync(token, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);
        await CacheActiveAsync(sessionHash, expirationDate, cancellationToken);

        return new CreateSsoSessionResult(sessionId, token);
    }

    public async Task<SsoSessionValidationResult> ValidateAsync(
        string sessionId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || userId == UserId.Empty)
        {
            return new SsoSessionValidationResult(false, Error: "Missing SSO session");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.CanLogin())
        {
            return new SsoSessionValidationResult(false, Error: "User cannot login");
        }

        var sessionHash = HashSessionId(sessionId);
        var cacheValue = await TryGetCacheValueAsync(CacheKey(sessionHash), cancellationToken);
        if (string.Equals(cacheValue, RevokedCacheValue, StringComparison.Ordinal))
        {
            return new SsoSessionValidationResult(false, Error: "SSO session revoked");
        }

        if (string.Equals(cacheValue, ActiveCacheValue, StringComparison.Ordinal))
        {
            return new SsoSessionValidationResult(true);
        }

        var token = await _tokenRepository.GetSsoSessionByReferenceIdAsync(sessionHash, cancellationToken);
        if (!IsValidToken(token, userId))
        {
            return new SsoSessionValidationResult(false, token, "SSO session is invalid");
        }

        await CacheActiveAsync(sessionHash, token!.ExpirationDate, cancellationToken);

        return new SsoSessionValidationResult(true, token);
    }

    public async Task RevokeAsync(
        string sessionId,
        UserId? userId = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        var sessionHash = HashSessionId(sessionId);
        var token = await _tokenRepository.GetSsoSessionByReferenceIdAsync(sessionHash, cancellationToken);

        if (token != null &&
            token.Type == OAuthConstants.TokenTypes.SsoSession &&
            (!userId.HasValue || token.UserId == userId.Value) &&
            token.Status == TokenStatus.Valid)
        {
            token.Revoke();
            _tokenRepository.Update(token);
            await _tokenRepository.SaveChangesAsync(cancellationToken);
        }

        await CacheRevokedAsync(sessionHash, token?.ExpirationDate, cancellationToken);
    }

    public async Task<bool> RevokeByIdAsync(
        TokenId tokenId,
        UserId userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _tokenRepository.GetByIdAsync(tokenId, cancellationToken);
        if (token == null ||
            token.UserId != userId ||
            token.Type != OAuthConstants.TokenTypes.SsoSession)
        {
            return false;
        }

        if (token.Status == TokenStatus.Valid)
        {
            token.Revoke();
            _tokenRepository.Update(token);
            await _tokenRepository.SaveChangesAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(token.ReferenceId))
        {
            await CacheRevokedAsync(token.ReferenceId, token.ExpirationDate, cancellationToken);
        }

        return true;
    }

    public async Task<int> RevokeAllByUserAsync(
        UserId userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _tokenRepository.GetActiveSsoSessionsByUserIdAsync(userId, cancellationToken);
        if (tokens.Count == 0)
        {
            return 0;
        }

        foreach (var token in tokens)
        {
            token.Revoke();
            _tokenRepository.Update(token);

            if (!string.IsNullOrWhiteSpace(token.ReferenceId))
            {
                await CacheRevokedAsync(token.ReferenceId, token.ExpirationDate, cancellationToken);
            }
        }

        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return tokens.Count;
    }

    public async Task<IReadOnlyList<Token>> GetActiveSessionsByUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return await _tokenRepository.GetActiveSsoSessionsByUserIdAsync(userId, cancellationToken);
    }

    public string HashSessionId(string sessionId)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sessionId));
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateSessionId()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    }

    private static bool IsValidToken(Token? token, UserId userId)
    {
        return token is
               {
                   Type: OAuthConstants.TokenTypes.SsoSession,
                   Status: TokenStatus.Valid
               } &&
               token.UserId == userId &&
               (!token.ExpirationDate.HasValue || token.ExpirationDate.Value > DateTime.UtcNow);
    }

    private async Task<string?> TryGetCacheValueAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await _cacheProvider.GetAsync(key, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task CacheActiveAsync(string sessionHash, DateTime? expiresAt,
        CancellationToken cancellationToken)
    {
        var ttl = GetPositiveTtl(expiresAt);
        if (!ttl.HasValue)
        {
            return;
        }

        try
        {
            await _cacheProvider.SetAsync(CacheKey(sessionHash), ActiveCacheValue, ttl, cancellationToken);
        }
        catch
        {
            // Redis is a speed/revocation cache; DB remains the source of truth.
        }
    }

    private async Task CacheRevokedAsync(string sessionHash, DateTime? expiresAt,
        CancellationToken cancellationToken)
    {
        var ttl = GetPositiveTtl(expiresAt) ?? RevokedFallbackTtl;
        try
        {
            await _cacheProvider.SetAsync(CacheKey(sessionHash), RevokedCacheValue, ttl, cancellationToken);
        }
        catch
        {
            // Revocation is persisted in DB; cache failure must not break logout/revoke.
        }
    }

    private static TimeSpan? GetPositiveTtl(DateTime? expiresAt)
    {
        if (!expiresAt.HasValue)
        {
            return null;
        }

        var ttl = expiresAt.Value - DateTime.UtcNow;
        return ttl > TimeSpan.Zero ? ttl : null;
    }

    private static string CacheKey(string sessionHash)
    {
        return $"sso:session:{sessionHash}";
    }
}
