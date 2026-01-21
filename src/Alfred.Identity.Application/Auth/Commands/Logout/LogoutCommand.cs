using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Logout;

/// <summary>
/// Command to logout a user (revoke refresh token)
/// </summary>
public record LogoutCommand(
    string RefreshToken
) : IRequest<LogoutResult>;

/// <summary>
/// Result of logout
/// </summary>
public record LogoutResult(
    bool Success,
    string? Error = null
);
