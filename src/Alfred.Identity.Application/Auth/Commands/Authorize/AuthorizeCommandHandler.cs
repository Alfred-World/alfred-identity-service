using System.Text.Json;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Common;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Authorize;

public class AuthorizeCommandHandler : IRequestHandler<AuthorizeCommand, AuthorizeResult>
{
    private readonly ITokenRepository _tokenRepository;
    private readonly IAuthorizationCodeService _authCodeService;

    private readonly IUnitOfWork _unitOfWork;
    // Assuming we have IApplicationRepository or similar, but typically IRepository<Application>
    // Since Application was added to Context, we can use Generic Repository or create specific one.
    // For now, let's assume we can access Applications via DbContext or IRepository if we had one.
    // But currently ITokenRepository is specific.
    // Let's use IUnitOfWork if we added Applications there? We added ITokenRepository to UnitOfWork. 
    // We haven't created ApplicationRepository yet.
    // Let's create IApplicationRepository or just use DbContext directly via UnitOfWork if feasible, 
    // but better to stick to Repo pattern.
    // I need to fetch Application by ClientId.

    // TEMPORARY: I will use ITokenRepository but I really need IApplicationRepository.
    // Let's assume I will create IApplicationRepository next.
    // For now I'll stub it or use a "Applications" dbSet directly if I can access context? No, clean architecture.
    // I'll create `IApplicationRepository` interface and implementation in this step too?
    // Let's define it here to be used.
    private readonly IApplicationRepository _applicationRepository;
    private readonly IAuthorizationRepository _authorizationRepository;

    public AuthorizeCommandHandler(
        ITokenRepository tokenRepository,
        IAuthorizationCodeService authCodeService,
        IApplicationRepository applicationRepository,
        IAuthorizationRepository authorizationRepository,
        IUnitOfWork unitOfWork)
    {
        _tokenRepository = tokenRepository;
        _authCodeService = authCodeService;
        _applicationRepository = applicationRepository;
        _authorizationRepository = authorizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthorizeResult> Handle(AuthorizeCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Client
        var client = await _applicationRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client == null)
        {
            return Error("invalid_client", "Client not found");
        }

        // 2. Validate Redirect URI
        // RedirectUris is stored as JSON array: ["url1","url2","url3"]
        var isValidRedirectUri = false;
        if (!string.IsNullOrEmpty(client.RedirectUris))
        {
            try
            {
                var redirectUris = JsonSerializer.Deserialize<string[]>(client.RedirectUris);
                isValidRedirectUri = redirectUris?.Contains(request.RedirectUri) ?? false;
            }
            catch
            {
                // Fallback: try space-delimited format
                isValidRedirectUri = client.RedirectUris.Split(' ').Contains(request.RedirectUri);
            }
        }

        if (!isValidRedirectUri)
        {
            return Error("invalid_request", "Invalid redirect_uri detected");
        }

        // 3. User Authentication
        if (request.UserId == null)
        {
            // User not authenticated, Controller should have redirected to Login
            // But if we are here, logic says we need user.
            // Typically Controller checks User.Identity.IsAuthenticated.
            // If not, it redirects to Login.
            // If we are here, we might return a result saying "Need Login"
            return new AuthorizeResult(false, Error: "login_required");
        }

        // 4. Generate Auth Code
        var code = _authCodeService.GenerateAuthorizationCode();
        var codeHash = _authCodeService.HashAuthorizationCode(code); // Optional hashing

        // 5. Create Authorization (Consent) - Implicit for now or check if exists
        // simplified: assuming implicit consent for internal apps or if ConsentType is implicit
        // For strictly following user request: "hoàn thiện logic"
        // Let's stick to creating a Token entity of type "authorization_code"

        // Note: Token entity has AuthorizationId. Ideally we create an Authorization entity first representing the grant.
        // Let's create an Authorization entity first.

        // 5. Create or Get Authorization (Consent)
        // Simplified: assuming implicit consent for now if no "prompt=consent"
        var authorization =
            await _authorizationRepository.GetValidAsync(client.Id, request.UserId.Value, request.Scope,
                cancellationToken);

        if (authorization == null)
        {
            authorization = Authorization.Create(
                client.Id,
                request.UserId.Value,
                request.Scope,
                "Permanent"
            );
            await _authorizationRepository.AddAsync(authorization, cancellationToken);
            await _authorizationRepository.SaveChangesAsync(cancellationToken); // Need SaveChanges in Repo or UoW
        }

        // 6. Create Authorization Code (Token entity)
        var codeValue = _authCodeService.GenerateAuthorizationCode();
        // Hash it? OpenIddict usually stores the code or a hash. 
        // Token entity has ReferenceId. Let's use ReferenceId for the Code string itself (or hash if we want to be secure).
        // If we hash, we return the plain code to user, and store hash.
        // Let's store hash in ReferenceId, and return plain code.

        var authTokenHash = _authCodeService.HashAuthorizationCode(codeValue);

        // Payload: could store code_challenge, redirect_uri, nonce, etc.
        var payload = JsonSerializer.Serialize(new
        {
            redirect_uri = request.RedirectUri,
            code_challenge = request.CodeChallenge,
            code_challenge_method = request.CodeChallengeMethod,
            // nonce should only be set if client sends a nonce parameter - NextAuth doesn't send one
            nonce = (string?)null,
            scope = request.Scope
        });

        // Create Token entity for the code
        var codeToken = Token.Create(
            OAuthConstants.TokenTypes.AuthorizationCode,
            client.Id,
            request.UserId.Value.ToString(),
            request.UserId.Value,
            DateTime.UtcNow.AddMinutes(5), // Short lived
            authTokenHash,
            authorization.Id,
            payload
        );

        await _tokenRepository.AddAsync(codeToken, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        // 7. Construct Redirect URI
        var delimiter = request.RedirectUri.Contains("?") ? "&" : "?";
        var redirectLocation = $"{request.RedirectUri}{delimiter}code={codeValue}";
        if (!string.IsNullOrEmpty(request.State))
        {
            redirectLocation += $"&state={request.State}";
        }

        return new AuthorizeResult(true, redirectLocation);
    }

    private AuthorizeResult Error(string error, string description)
    {
        // Should redirect back to redirect_uri with error if possible, unless redirect_uri is invalid
        // logic is complex. For API, return object.
        return new AuthorizeResult(false, Error: error, ErrorDescription: description);
    }
}
