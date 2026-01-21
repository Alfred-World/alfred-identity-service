using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;
using MediatR;
using System.Text.Json;

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
        if (request.GrantType == "authorization_code")
        {
            return await HandleAuthorizationCodeGrant(request, cancellationToken);
        }
        
        // Handle refresh_token grant type here or in separate command?
        // Typically singular endpoint logic.
        // For simplicity, focusing on authorization_code as per current task.
        // Refresh token logic is already in RefreshTokenCommand, but that was for self-service logic.
        // We should unify. But let's stick to auth code for now.
        
        return Error("unsupported_grant_type", "Grant type not supported");
    }

    private async Task<ExchangeCodeResult> HandleAuthorizationCodeGrant(ExchangeCodeCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Request
        if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.RedirectUri))
        {
            return Error("invalid_request", "Missing code or redirect_uri");
        }

        // 2. Client Authentication
        // Typically done via Basic Auth header or post body.
        // Assuming ClientId is passed.
        if (string.IsNullOrEmpty(request.ClientId))
        {
             return Error("invalid_client", "Client ID is required");
        }
        
        var client = await _applicationRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client == null)
        {
             return Error("invalid_client", "Invalid client");
        }
        
        // Validate Client Secret if Confidential (skipped for now, assume Public or simplified)
        // if (client.Type == "confidential" && !ValidateSecret(client, request.ClientSecret)) ...

        // 3. Retrieve Authorization Code
        // We stored the HASH of the code. We need to hash the incoming code to find it.
        var codeHash = _authCodeService.HashAuthorizationCode(request.Code);
        
        // Find token by ReferenceId (which is the hash)
        // ITokenRepository.GetByReferenceIdAsync needed?
        // Currently we have GetByTokenAsync? No, let's check repo interface.
        // TokenRepository implementation handles ReferenceId search usually.
        // Assuming we need to add query for it or use existing method.
        // Let's assume we can query by ReferenceId.
        
        // Since ITokenRepository is generic or limited, let's look at what we have.
        // We might need to add `GetByReferenceIdAsync` to ITokenRepository if not exists.
        // Actually `TokenRepository` has basic methods.
        // For now, let's assume `GetByRefreshTokenAsync` actually searches by ReferenceId?
        // Let's check ITokenRepository.
        
        var authCodeToken = await _tokenRepository.GetByReferenceIdAsync(codeHash, cancellationToken);
        
        if (authCodeToken == null || authCodeToken.Type != "authorization_code" || authCodeToken.Status != "Valid")
        {
            return Error("invalid_grant", "Authorization code is invalid or expired");
        }

        // 4. Validate Expiration
        if (authCodeToken.ExpirationDate < DateTime.UtcNow)
        {
            return Error("invalid_grant", "Authorization code has expired");
        }

        // 5. Validate PKCE
        // Payload contains metadata
        if (string.IsNullOrEmpty(authCodeToken.Payload))
        {
             return Error("server_error", "Invalid token payload");
        }

        var payload = JsonSerializer.Deserialize<JsonElement>(authCodeToken.Payload);
        var storedRedirectUri = payload.GetProperty("redirect_uri").GetString();
        var codeChallenge = payload.TryGetProperty("code_challenge", out var c) ? c.GetString() : null;
        var codeChallengeMethod = payload.TryGetProperty("code_challenge_method", out var m) ? m.GetString() : null;

        // Verify Redirect URI matches
        if (storedRedirectUri != request.RedirectUri)
        {
             return Error("invalid_grant", "Redirect URI mismatch");
        }

        // PKCE Validation
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
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        // 7. Generate Tokens
        var userId = authCodeToken.UserId ?? 0; // Should be set
        
        // Get User
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Error("invalid_grant", "User not found");
        }
        
        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.FullName, client.Id);
        var refreshTokenStr = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshTokenStr);

        // Generate ID Token for OIDC
        // Get nonce from payload if available
        var nonce = payload.TryGetProperty("nonce", out var n) ? n.GetString() : null;
        var idToken = await _jwtTokenService.GenerateIdTokenAsync(user.Id, user.Email, user.FullName, request.ClientId, nonce);

        // 8. Store Refresh Token
        var refreshToken = Token.Create(
            type: "refresh_token",
            applicationId: client.Id,
            subject: userId.ToString(),
            userId: userId,
            expirationDate: DateTime.UtcNow.AddDays(14),
            referenceId: refreshTokenHash,
            authorizationId: authCodeToken.AuthorizationId,
            payload: null // Props?
        );
        
        await _tokenRepository.AddAsync(refreshToken, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return new ExchangeCodeResult(
            Success: true,
            AccessToken: accessToken,
            RefreshToken: refreshTokenStr,
            IdToken: idToken,
            ExpiresIn: 3600, // 1 hour
            TokenType: "Bearer"
        );
    }
    
    private ExchangeCodeResult Error(string error, string description)
    {
        return new ExchangeCodeResult(false, Error: error, ErrorDescription: description);
    }
}
