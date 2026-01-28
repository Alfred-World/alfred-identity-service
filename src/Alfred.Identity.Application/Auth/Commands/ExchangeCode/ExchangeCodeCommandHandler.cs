using System.Text.Json;

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
    private readonly IUserRepository _userRepository; // Added
    private readonly IAuthorizationCodeService _authCodeService;
    private readonly IJwtTokenService _jwtTokenService;

    public ExchangeCodeCommandHandler(
        ITokenRepository tokenRepository,
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        IAuthorizationCodeService authCodeService,
        IJwtTokenService jwtTokenService)
    {
        _tokenRepository = tokenRepository;
        _applicationRepository = applicationRepository;
        _userRepository = userRepository;
        _authCodeService = authCodeService;
        _jwtTokenService = jwtTokenService;
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
        var userId = authCodeToken.UserId ?? 0;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Error("invalid_grant", "User not found");
        }

        var accessToken =
            await _jwtTokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.FullName, client.Id);
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
            null
        );

        await _tokenRepository.AddAsync(refreshToken, cancellationToken);

        // Single SaveChanges for atomic operation
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return new ExchangeCodeResult(
            true,
            accessToken,
            refreshTokenStr,
            idToken,
            ExpiresIn: 3600, // 1 hour
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
        if (tokenEntity.Status != TokenStatus.Valid)
        {
            // If reusing revoked token -> security risk -> revoke all?
            // For now, simplify to error.
            return Error(OAuthConstants.Errors.InvalidGrant, "Refresh token has been reused or revoked");
        }

        if (tokenEntity.ExpirationDate < DateTime.UtcNow)
        {
            return Error("invalid_grant", "Refresh token expired");
        }

        // 5. Rotate Refresh Token
        // Revoke current token
        tokenEntity.Redeem();
        _tokenRepository.Update(tokenEntity);

        // 6. Generate New Tokens
        var userId = tokenEntity.UserId ?? 0;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Error("invalid_grant", "User not found");
        }

        var newAccessToken =
            await _jwtTokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.FullName, client.Id);

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
            null
        );

        await _tokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        // Atomic transaction
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return new ExchangeCodeResult(
            true,
            newAccessToken,
            newRefreshTokenStr,
            newIdToken,
            ExpiresIn: 3600,
            TokenType: "Bearer"
        );
    }

    private ExchangeCodeResult Error(string error, string description)
    {
        return new ExchangeCodeResult(false, Error: error, ErrorDescription: description);
    }
}
