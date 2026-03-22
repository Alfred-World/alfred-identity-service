namespace Alfred.Identity.Application.Users.Common;

public sealed record CreateUserInput(
    string Email,
    string Password,
    string FullName,
    string? UserName,
    IEnumerable<Guid>? RoleIds
);
