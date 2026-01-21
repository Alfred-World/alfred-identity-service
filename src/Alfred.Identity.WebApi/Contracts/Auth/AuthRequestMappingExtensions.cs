using Alfred.Identity.Application.Auth.Commands.Login;

namespace Alfred.Identity.WebApi.Contracts.Auth;

/// <summary>
/// Extension methods for mapping auth requests to commands
/// </summary>
public static class AuthRequestMappingExtensions
{
    // Note: SSO Login uses LoginCommand directly in the controller
    // No mapping extensions needed since it's only used in one place
}

