using System.Text.Json;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Common.Enums;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ExchangeCode;

public class ExchangeCodeCommandHandler : IRequestHandler<ExchangeCodeCommand, ExchangeCodeResult>
{
    private readonly ITokenRepository _tokenRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuthorizationCodeService _authCodeService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICacheProvider _cacheProvider;

    public ExchangeCodeCommandHandler(
        ITokenRepository tokenRepository,
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        IAuthorizationCodeService authCodeService,
        IJwtTokenService jwtTokenService,
        ICacheProvider cacheProvider)
    {
        _tokenRepository = tokenRepository;
        _applicationRepository = applicationRepository;
        _userRepository = userRepository;
        _authCodeService = authCodeService;
        _jwtTokenService = jwtTokenService;
        _cacheProvider = cacheProvider;
    }

    // ... (Handle method remains)


    public async Task<ExchangeCodeResult> Handle(ExchangeCodeCommand request, CancellationToken cancellationToken)
    {
        if (request.GrantType == OAuthConstants.GrantTypes.AuthorizationCode)
        {
            return await HandleAuthorizationCodeGrant(request, cancellationToken);
        }

        if (request.GrantType == OAuthConstants.GrantTypes.RefreshToken)
        {
            return await HandleRefreshTokenGrant(request, cancellationToken);
        }

        return Error(OAuthConstants.Errors.UnsupportedGrantType, "Grant type not supported");
    }

    private async Task<ExchangeCodeResult> HandleAuthorizationCodeGrant(ExchangeCodeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate Request
        if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.RedirectUri))
        {
            return Error("invalid_request", "Missing code or redirect_uri");
        }

        // 2. Client Authentication
        if (string.IsNullOrEmpty(request.ClientId))
        {
            return Error("invalid_client", "Client ID is required");
        }

        var client = await _applicationRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client == null)
        {
            return Error("invalid_client", "Invalid client");
        }

        // 3. Retrieve Authorization Code
        var codeHash = _authCodeService.HashAuthorizationCode(request.Code);
        var authCodeToken = await _tokenRepository.GetByReferenceIdAsync(codeHash, cancellationToken);

        if (authCodeToken == null || authCodeToken.Type != OAuthConstants.TokenTypes.AuthorizationCode ||
            authCodeToken.Status != TokenStatus.Valid)
        {
            return Error(OAuthConstants.Errors.InvalidGrant, "Authorization code is invalid or expired");
        }

        // 4. Validate Expiration
        if (authCodeToken.ExpirationDate < DateTime.UtcNow)
        {
            return Error("invalid_grant", "Authorization code has expired");
        }

        // 5. Validate PKCE
        if (string.IsNullOrEmpty(authCodeToken.Payload))
        {
            return Error("server_error", "Invalid token payload");
        }

        var payload = JsonSerializer.Deserialize<JsonElement>(authCodeToken.Payload);
        var storedRedirectUri = payload.GetProperty("redirect_uri").GetString();
        var codeChallenge = payload.TryGetProperty("code_challenge", out var c) ? c.GetString() : null;
        var codeChallengeMethod = payload.TryGetProperty("code_challenge_method", out var m) ? m.GetString() : null;

        if (storedRedirectUri != request.RedirectUri)
        {
            return Error("invalid_grant", "Redirect URI mismatch");
        }

        if (!string.IsNullOrEmpty(codeChallenge))
        {
            if (string.IsNullOrEmpty(request.CodeVerifier))
            {
                return Error("invalid_request", "Code verifier is missing");
            }

            if (!_authCodeService.ValidatePkce(codeChallenge, codeChallengeMethod ?? "S256", request.CodeVerifier))
            {
                return Error("invalid_grant", "PKCE verification failed");
            }
        }

        // 6. Redeem Code (Burn it)
        authCodeToken.Redeem();
        _tokenRepository.Update(authCodeToken);

        // 7. Generate Tokens
        var userId = authCodeToken.UserId ?? Guid.Empty;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Error("invalid_grant", "User not found");
        }

        var accessToken =
            await _jwtTokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.FullName, client.Id,
                authCodeToken.AuthorizationId);
        var refreshTokenStr = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshTokenStr);

        var nonce = payload.TryGetProperty("nonce", out var n) ? n.GetString() : null;
        var idToken =
            await _jwtTokenService.GenerateIdTokenAsync(user.Id, user.Email, user.FullName, request.ClientId, nonce);

        // 8. Store Refresh Token
        var refreshToken = Token.Create(
            OAuthConstants.TokenTypes.RefreshToken,
            client.Id,
            userId.ToString(),
            userId,
            DateTime.UtcNow.AddDays(14),
            refreshTokenHash,
            authCodeToken.AuthorizationId,
            null,
            ipAddress: request.IpAddress,
            device: request.Device
        );

        await _tokenRepository.AddAsync(refreshToken, cancellationToken);

        // Single SaveChanges for atomic operation
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return new ExchangeCodeResult(
            true,
            accessToken,
            refreshTokenStr,
            idToken,
            ExpiresIn: _jwtTokenService.AccessTokenLifetimeSeconds,
            TokenType: "Bearer"
        );
    }

    private async Task<ExchangeCodeResult> HandleRefreshTokenGrant(ExchangeCodeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate Request
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return Error("invalid_request", "Missing refresh_token");
        }

        // 2. Client Authentication
        if (string.IsNullOrEmpty(request.ClientId))
        {
            return Error("invalid_client", "Client ID is required");
        }

        var client = await _applicationRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client == null)
        {
            return Error("invalid_client", "Invalid client");
        }

        // 3. Retrieve Refresh Token
        // Hash incoming refresh token to find it
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var tokenEntity = await _tokenRepository.GetByReferenceIdAsync(refreshTokenHash, cancellationToken);

        if (tokenEntity == null || tokenEntity.Type != OAuthConstants.TokenTypes.RefreshToken)
        {
            return Error(OAuthConstants.Errors.InvalidGrant, "Invalid refresh token");
        }

        // 4. Validate Token Usage
        // Track grace-period reuse to skip calling Redeem() again on an already-Redeemed token.
        // Calling Redeem() resets RedemptionDate to "now", which would shift the createdAfter
        // window and cause subsequent parallel requests to miss the new RT (invalid_grant).
        var isGracePeriodReuse = false;
        if (tokenEntity.Status != TokenStatus.Valid)
        {
            // Grace Period: if token was just redeemed (< 60s ago) a concurrent request already rotated it.
            // Instead of issuing yet another new token (which causes DB bloat), find the one that was
            // already created for this AuthorizationId and return it. This prevents the race condition
            // where NextAuth fires multiple parallel refresh requests at startup.
            var gracePeriodSeconds = 60;
            if (tokenEntity.Status == TokenStatus.Redeemed &&
                tokenEntity.RedemptionDate.HasValue &&
                tokenEntity.RedemptionDate.Value > DateTime.UtcNow.AddSeconds(-gracePeriodSeconds) &&
                tokenEntity.AuthorizationId.HasValue)
            {
                // Parallel request race: RT was already redeemed by a concurrent request.
                // Do NOT query/revoke the new RT that was issued by request A — if we revoke it
                // and request A's cookie arrives after ours, the cookie has a Revoked RT → invalid_grant.
                // Instead let both new RTs briefly coexist. Whichever the cookie ends up with is valid.
                // Orphaned Valid RTs expire naturally (14 days) and are cleaned up by DeleteExpiredAndRedeemed.
                isGracePeriodReuse = true;
            }
            else
            {
                return Error(OAuthConstants.Errors.InvalidGrant, "Refresh token has been reused or revoked");
            }
        }

        if (tokenEntity.ExpirationDate < DateTime.UtcNow)
        {
            return Error("invalid_grant", "Refresh token expired");
        }

        // Check if session was explicitly revoked via Redis (immediate revoke from session management)
        if (await _cacheProvider.ExistsAsync($"session:revoked:{tokenEntity.Id}", cancellationToken))
        {
            return Error(OAuthConstants.Errors.InvalidGrant, "Session has been revoked");
        }

        // 5. Rotate Refresh Token
        // Use RedeemByIdAsync (ExecuteUpdateAsync) instead of change-tracking Update().
        // This is a direct SQL UPDATE that never causes DbUpdateConcurrencyException:
        //   - If concurrent request already redeemed this token: 0 rows, no exception.
        //   - If cleanup deleted the row: 0 rows, no exception.
        //   - Normal case: 1 row updated, RedemptionDate set.
        if (!isGracePeriodReuse)
        {
            await _tokenRepository.RedeemByIdAsync(tokenEntity.Id, cancellationToken);
        }

        // 6. Generate New Tokens
        var userId = tokenEntity.UserId ?? Guid.Empty;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Error("invalid_grant", "User not found");
        }

        var newAccessToken =
            await _jwtTokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.FullName, client.Id,
                tokenEntity.AuthorizationId);

        // Generate new Refresh Token (Rotation)
        var newRefreshTokenStr = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshTokenStr);

        // ID Token (optional for refresh flow, but good for updating claims)
        var newIdToken =
            await _jwtTokenService.GenerateIdTokenAsync(user.Id, user.Email, user.FullName, client.ClientId);

        var newRefreshTokenEntity = Token.Create(
            OAuthConstants.TokenTypes.RefreshToken,
            client.Id,
            userId.ToString(),
            userId,
            DateTime.UtcNow.AddDays(14), // Extend session
            newRefreshTokenHash,
            tokenEntity.AuthorizationId,
            null,
            ipAddress: request.IpAddress,
            device: request.Device
        );

        await _tokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        // SaveChangesAsync now only handles the new RT INSERT (no change-tracked updates).
        // RedeemByIdAsync above used ExecuteUpdateAsync which auto-commits independently.
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        // Clean up expired/redeemed/revoked tokens for this user (fire-and-forget).
        _ = _tokenRepository.DeleteExpiredAndRedeemedByUserAsync(userId, CancellationToken.None);

        return new ExchangeCodeResult(
            true,
            newAccessToken,
            newRefreshTokenStr,
            newIdToken,
            ExpiresIn: _jwtTokenService.AccessTokenLifetimeSeconds,
            TokenType: "Bearer"
        );
    }

    private ExchangeCodeResult Error(string error, string description)
    {
        return new ExchangeCodeResult(false, Error: error, ErrorDescription: description);
    }
}
