using System.Text.Json;

using Alfred.Identity.Application.Auth.Common;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Authorize;

public class AuthorizeCommandHandler : IRequestHandler<AuthorizeCommand, AuthorizeResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthorizationCodeService _authCodeService;

    public AuthorizeCommandHandler(
        IUnitOfWork unitOfWork,
        IAuthorizationCodeService authCodeService)
    {
        _unitOfWork = unitOfWork;
        _authCodeService = authCodeService;
    }

    public async Task<AuthorizeResult> Handle(AuthorizeCommand request, CancellationToken cancellationToken)
    {
        var client = await _unitOfWork.Applications.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client is not { IsActive: true })
        {
            return Error("invalid_client", "Client not found or inactive");
        }

        if (!client.RedirectUris.Contains(request.RedirectUri))
        {
            return Error("invalid_request", "Invalid redirect_uri detected");
        }

        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal))
        {
            return Error("unsupported_response_type", "Only response_type=code is supported");
        }

        if (!OidcClientPermissions.SupportsEndpoint(client, ApplicationConstants.Endpoints.Authorization))
        {
            return Error(OAuthConstants.Errors.UnauthorizedClient,
                "Client is not allowed to use the authorization endpoint");
        }

        if (!OidcClientPermissions.SupportsGrantType(client, OAuthConstants.GrantTypes.AuthorizationCode))
        {
            return Error(OAuthConstants.Errors.UnauthorizedClient,
                "Client is not allowed to use the authorization_code grant");
        }

        if (!OidcClientPermissions.AreScopesAllowed(client, request.Scope, out var unsupportedScopes))
        {
            return Error(OAuthConstants.Errors.InvalidScope,
                $"Unsupported scope(s): {string.Join(", ", unsupportedScopes)}");
        }

        if (!string.IsNullOrEmpty(request.CodeChallenge) || !string.IsNullOrEmpty(request.CodeChallengeMethod))
        {
            if (string.IsNullOrEmpty(request.CodeChallenge) || request.CodeChallengeMethod != "S256")
            {
                return Error(OAuthConstants.Errors.InvalidRequest,
                    "Only code_challenge_method=S256 is supported when PKCE is supplied");
            }
        }

        if (request.UserId == null)
        {
            return new AuthorizeResult(false, Error: "login_required");
        }

        var typedUserId = new UserId(request.UserId.Value);
        string? redirectLocation = null;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var authorization =
                await _unitOfWork.Authorizations.GetValidAsync(client.Id, typedUserId, request.Scope, ct);

            if (authorization == null)
            {
                authorization = Authorization.Create(
                    client.Id,
                    typedUserId,
                    request.Scope,
                    "Permanent"
                );
                await _unitOfWork.Authorizations.AddAsync(authorization, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            var codeValue = _authCodeService.GenerateAuthorizationCode();
            var authTokenHash = _authCodeService.HashAuthorizationCode(codeValue);

            var payload = JsonSerializer.Serialize(new
            {
                redirect_uri = request.RedirectUri,
                code_challenge = request.CodeChallenge,
                code_challenge_method = request.CodeChallengeMethod,
                nonce = (string?)null,
                scope = request.Scope
            });

            var codeToken = Token.Create(
                OAuthConstants.TokenTypes.AuthorizationCode,
                client.Id,
                request.UserId.Value.ToString(),
                typedUserId,
                DateTime.UtcNow.AddMinutes(5),
                authTokenHash,
                authorization.Id,
                payload,
                ipAddress: request.IpAddress,
                device: request.Device
            );

            await _unitOfWork.Tokens.AddAsync(codeToken, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var delimiter = request.RedirectUri.Contains('?') ? "&" : "?";
            redirectLocation = $"{request.RedirectUri}{delimiter}code={codeValue}";
            if (!string.IsNullOrEmpty(request.State))
            {
                redirectLocation += $"&state={request.State}";
            }
        }, cancellationToken);

        return new AuthorizeResult(true, redirectLocation);
    }

    private AuthorizeResult Error(string error, string description)
    {
        return new AuthorizeResult(false, Error: error, ErrorDescription: description);
    }
}
