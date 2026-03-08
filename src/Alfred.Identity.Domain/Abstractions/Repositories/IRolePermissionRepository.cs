using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

/// <summary>
/// Read-only repository for RolePermission lookup (used by PermissionCacheService).
/// Write operations on RolePermission are handled via Role entity aggregate.
/// </summary>
public interface IRolePermissionRepository
{
    Task<IEnumerable<Permission>> GetPermissionsByRoleNameAsync(string roleName,
        CancellationToken cancellationToken = default);
}
