namespace Alfred.Identity.WebApi.Contracts.Roles;

public sealed record CreateRoleRequest(
    string Name,
    string? Icon = null,
    bool IsImmutable = false,
    bool IsSystem = false,
    List<Guid>? Permissions = null);

public sealed record UpdateRoleRequest
{
    public Optional<string> Name { get; init; }
    public Optional<string?> Icon { get; init; }
    public Optional<bool> IsImmutable { get; init; }
    public Optional<bool> IsSystem { get; init; }
    public Optional<List<Guid>?> Permissions { get; init; }
}
