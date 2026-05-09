using System.Text.Json;

using Alfred.Identity.Application.Auth.Common;
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
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuthorizationCodeService _authCodeService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICacheProvider _cacheProvider;
    private readonly IClientSecretHasher _clientSecretHasher;

    public ExchangeCodeCommandHandler(
        ITokenRepository tokenRepository,
        IApplicationRepository applicationRepository,
        IAuthorizationRepository authorizationRepository,
        IUserRepository userRepository,
        IAuthorizationCodeService authCodeService,
        IJwtTokenService jwtTokenService,
        ICacheProvider cacheProvider,
        IClientSecretHasher clientSecretHasher)
    {
        _tokenRepository = tokenRepository;
        _applicationRepository = applicationRepository;
        _authorizationRepository = authorizationRepository;
        _userRepository = userRepository;
        _authCodeService = authCodeService;
        _jwtTokenService = jwtTokenService;
        _cacheProvider = cacheProvider;
        _clientSecretHasher = clientSecretHasher;
    }

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
        if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.RedirectUri))
        {
            return Error("invalid_request", "Missing code or redirect_uri");
        }

        var clientResult = await ValidateClientAsync(request, OAuthConstants.GrantTypes.AuthorizationCode,
            cancellationToken);
        if (!clientResult.Success)
        {
            return Error(clientResult.Error!, clientResult.Description!);
        }

        var client = clientResult.Client!;

        var codeHash = _authCodeService.HashAuthorizationCode(request.Code);
        var authCodeToken = await _tokenRepository.GetByReferenceIdAsync(codeHash, cancellationToken);

        if (authCodeToken == null || authCodeToken.Type != OAuthConstants.TokenTypes.AuthorizationCode ||
            authCodeToken.Status != TokenStatus.Valid)
        {
            return Error(OAuthConstants.Errors.InvalidGrant, "Authorization code is invalid or expired");
        }

        if (authCodeToken.ApplicationId != client.Id)
        {
            return Error(OAuthConstants.Errors.InvalidGrant, "Authorization code does not belong to this client");
        }

        if (authCodeToken.ExpirationDate < DateTime.UtcNow)
        {
            return Error("invalid_grant", "Authorization code has expired");
        }

        if (string.IsNullOrEmpty(authCodeToken.Payload))
        {
            return Error("server_error", "Invalid token payload");
        }

        var payload = JsonSerializer.Deserialize<JsonElement>(authCodeToken.Payload);
        var storedRedirectUri = payload.GetProperty("redirect_uri").GetString();
        var codeChallenge = payload.TryGetProperty("code_challenge", out var c) ? c.GetString() : null;
        var codeChallengeMethod = payload.TryGetProperty("code_challenge_method", out var m) ? m.GetString() : null;
        var requestedScopes = payload.TryGetProperty("scope", out var scope) ? scope.GetString() : null;

        if (storedRedirectUri != request.RedirectUri)
        {
            return Error("invalid_grant", "Redirect URI mismatch");
        }

        if (!OidcClientPermissions.AreScopesAllowed(client, requestedScopes, out var unsupportedScopes))
        {
            return Error(OAuthConstants.Errors.InvalidScope,
                $"Unsupported scope(s): {string.Join(", ", unsupportedScopes)}");
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

        authCodeToken.Redeem();
        _tokenRepository.Update(authCodeToken);

        var userId = authCodeToken.UserId ?? UserId.Empty;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.CanLogin())
        {
            return Error("invalid_grant", "User not found or inactive");
        }

        var accessToken =
            await _jwtTokenService.GenerateAccessTokenAsync(user.Id.Value, user.Email, user.FullName, client.Id.Value,
                client.ClientId, authCodeToken.AuthorizationId?.Value, requestedScopes);

        var nonce = payload.TryGetProperty("nonce", out var n) ? n.GetString() : null;
        string? idToken = null;
        if (OidcClientPermissions.ContainsScope(requestedScopes, "openid"))
        {
            idToken =
                await _jwtTokenService.GenerateIdTokenAsync(user.Id.Value, user.Email, user.FullName,
                    request.ClientId!, nonce);
        }

        string? refreshTokenStr = null;
        if (OidcClientPermissions.SupportsGrantType(client, OAuthConstants.GrantTypes.RefreshToken) &&
            OidcClientPermissions.ContainsScope(requestedScopes, "offline_access"))
        {
            refreshTokenStr = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshTokenStr);

            var refreshToken = Token.Create(
                OAuthConstants.TokenTypes.RefreshToken,
                client.Id,
                userId.ToString(),
                userId,
                DateTime.UtcNow.AddSeconds(_jwtTokenService.RefreshTokenLifetimeSeconds),
                refreshTokenHash,
                authCodeToken.AuthorizationId,
                null,
                ipAddress: authCodeToken.IpAddress,
                device: authCodeToken.Device
            );

            await _tokenRepository.AddAsync(refreshToken, cancellationToken);
        }

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
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return Error("invalid_request", "Missing refresh_token");
        }

        var clientResult = await ValidateClientAsync(request, OAuthConstants.GrantTypes.RefreshToken,
            cancellationToken);
        if (!clientResult.Success)
        {
            return Error(clientResult.Error!, clientResult.Description!);
        }

        var client = clientResult.Client!;

        var refreshTokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var tokenEntity = await _tokenRepository.GetByReferenceIdAsync(refreshTokenHash, cancellationToken);

        if (tokenEntity == null || tokenEntity.Type != OAuthConstants.TokenTypes.RefreshToken)
        {
            return Error(OAuthConstants.Errors.InvalidGrant, "Invalid refresh token");
        }

        if (tokenEntity.ApplicationId != client.Id)
        {
            return Error(OAuthConstants.Errors.InvalidGrant, "Refresh token does not belong to this client");
        }

        var isGracePeriodReuse = false;
        if (tokenEntity.Status != TokenStatus.Valid)
        {
            var gracePeriodSeconds = 60;
            if (tokenEntity.Status == TokenStatus.Redeemed &&
                tokenEntity.RedemptionDate.HasValue &&
                tokenEntity.RedemptionDate.Value > DateTime.UtcNow.AddSeconds(-gracePeriodSeconds) &&
                tokenEntity.AuthorizationId.HasValue)
            {
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

        if (await _cacheProvider.ExistsAsync($"session:revoked:{tokenEntity.Id}", cancellationToken))
        {
            return Error(OAuthConstants.Errors.InvalidGrant, "Session has been revoked");
        }

        if (tokenEntity.AuthorizationId.HasValue &&
            await _cacheProvider.ExistsAsync($"revoked:session:{tokenEntity.AuthorizationId.Value}",
                cancellationToken))
        {
            return Error(OAuthConstants.Errors.InvalidGrant, "Session has been revoked");
        }

        if (!isGracePeriodReuse)
        {
            await _tokenRepository.RedeemByIdAsync(tokenEntity.Id, cancellationToken);
        }

        var userId = tokenEntity.UserId ?? UserId.Empty;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.CanLogin())
        {
            return Error("invalid_grant", "User not found or inactive");
        }

        string? authorizationScopes = null;
        if (tokenEntity.AuthorizationId.HasValue)
        {
            var authorization = await _authorizationRepository.GetByIdAsync(tokenEntity.AuthorizationId.Value,
                cancellationToken);
            authorizationScopes = authorization?.Scopes;
        }

        var newAccessToken =
            await _jwtTokenService.GenerateAccessTokenAsync(user.Id.Value, user.Email, user.FullName, client.Id.Value,
                client.ClientId, tokenEntity.AuthorizationId?.Value, authorizationScopes);

        var newRefreshTokenStr = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshTokenStr);
        var newRefreshTokenDevice = ResolveRefreshDevice(request.Device, tokenEntity.Device);

        string? newIdToken = null;
        if (OidcClientPermissions.ContainsScope(authorizationScopes, "openid"))
        {
            newIdToken =
                await _jwtTokenService.GenerateIdTokenAsync(user.Id.Value, user.Email, user.FullName,
                    client.ClientId);
        }

        var newRefreshTokenEntity = Token.Create(
            OAuthConstants.TokenTypes.RefreshToken,
            client.Id,
            userId.ToString(),
            userId,
            DateTime.UtcNow.AddSeconds(_jwtTokenService.RefreshTokenLifetimeSeconds),
            newRefreshTokenHash,
            tokenEntity.AuthorizationId,
            null,
            ipAddress: request.IpAddress,
            device: newRefreshTokenDevice
        );

        await _tokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _tokenRepository.DeleteExpiredAndRedeemedByUserAsync(userId, CancellationToken.None);
        }
        catch
        {
            /* non-critical cleanup */
        }

        return new ExchangeCodeResult(
            true,
            newAccessToken,
            newRefreshTokenStr,
            newIdToken,
            ExpiresIn: _jwtTokenService.AccessTokenLifetimeSeconds,
            TokenType: "Bearer"
        );
    }

    private async Task<ClientValidationResult> ValidateClientAsync(ExchangeCodeCommand request, string grantType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ClientId))
        {
            return ClientError("invalid_client", "Client ID is required");
        }

        var client = await _applicationRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client is not { IsActive: true })
        {
            return ClientError("invalid_client", "Invalid client");
        }

        if (!OidcClientPermissions.SupportsEndpoint(client, ApplicationConstants.Endpoints.Token))
        {
            return ClientError(OAuthConstants.Errors.UnauthorizedClient,
                "Client is not allowed to use the token endpoint");
        }

        if (!OidcClientPermissions.SupportsGrantType(client, grantType))
        {
            return ClientError(OAuthConstants.Errors.UnauthorizedClient,
                $"Client is not allowed to use the {grantType} grant");
        }

        if (client.ClientType?.Equals(ApplicationConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase) == true)
        {
            if (string.IsNullOrEmpty(request.ClientSecret) || string.IsNullOrEmpty(client.ClientSecret))
            {
                return ClientError("invalid_client", "Client secret is required for confidential clients");
            }

            if (!_clientSecretHasher.VerifySecret(request.ClientSecret, client.ClientSecret))
            {
                return ClientError("invalid_client", "Invalid client secret");
            }
        }

        return new ClientValidationResult(true, client);
    }

    private static ClientValidationResult ClientError(string error, string description)
    {
        return new ClientValidationResult(false, Error: error, Description: description);
    }

    private ExchangeCodeResult Error(string error, string description)
    {
        return new ExchangeCodeResult(false, Error: error, ErrorDescription: description);
    }

    private static string? ResolveRefreshDevice(string? requestDevice, string? existingDevice)
    {
        if (string.IsNullOrWhiteSpace(requestDevice) || IsServerSideUserAgent(requestDevice))
        {
            return existingDevice ?? requestDevice;
        }

        return requestDevice;
    }

    private static bool IsServerSideUserAgent(string device)
    {
        var normalized = device.Trim();

        return normalized.Equals("node", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("undici", StringComparison.OrdinalIgnoreCase) ||
               normalized.StartsWith("NextAuth.js", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ClientValidationResult(
        bool Success,
        Domain.Entities.Application? Client = null,
        string? Error = null,
        string? Description = null);
}
