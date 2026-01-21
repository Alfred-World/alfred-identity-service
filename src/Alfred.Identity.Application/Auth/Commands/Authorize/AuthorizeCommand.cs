using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Authorize;

/// <summary>
/// Command to authorize a user and generate an authorization code (OAuth2)
/// </summary>
public record AuthorizeCommand(
    string ClientId,
    string RedirectUri,
    string ResponseType,
    string Scope,
    string? State,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? Prompt,
    long? UserId = null // If user is already authenticated
) : IRequest<AuthorizeResult>;

/// <summary>
/// Result of authorization request
/// </summary>
public record AuthorizeResult(
    bool Success,
    string? RedirectLocation = null, // The fully formed redirect URI with code/state
    string? View = null, // If consent/login is needed, return view name ? (Or handle at controller)
    string? Error = null,
    string? ErrorDescription = null
);
