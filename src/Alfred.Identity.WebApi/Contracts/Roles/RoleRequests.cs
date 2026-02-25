namespace Alfred.Identity.WebApi.Contracts.Roles;

public sealed record CreateRoleRequest(
    string Name,
    string? Icon = null,
    bool IsImmutable = false,
    bool IsSystem = false,
    List<Guid>? Permissions = null);

public sealed record UpdateRoleRequest(
    string Name,
    string? Icon = null,
    bool IsImmutable = false,
    bool IsSystem = false,
    List<Guid>? Permissions = null);
