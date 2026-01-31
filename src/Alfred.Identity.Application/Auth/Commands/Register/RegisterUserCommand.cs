using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Register;

/// <summary>
/// Command to register a new user
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FullName
) : IRequest<RegisterUserResult>;

/// <summary>
/// Result of user registration
/// </summary>
public record RegisterUserResult(
    bool Success,
    Guid? UserId = null,
    string? Error = null
);
