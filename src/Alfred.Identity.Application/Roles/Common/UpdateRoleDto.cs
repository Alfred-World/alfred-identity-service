namespace Alfred.Identity.Application.Roles.Common;

public sealed record UpdateRoleDto
{
    public Optional<string> Name { get; init; }
    public Optional<string?> Icon { get; init; }
    public Optional<bool> IsImmutable { get; init; }
    public Optional<bool> IsSystem { get; init; }
    public Optional<List<Guid>?> Permissions { get; init; }
}
