namespace Alfred.Identity.WebApi.Contracts.Users;

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string FullName,
    string? UserName,
    List<Guid>? RoleIds
);
