using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

/// <summary>
/// Repository interface for RolePermission entity (many-to-many join table)
/// </summary>
public interface IRolePermissionRepository
{
    Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(RoleId roleId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Permission>> GetPermissionsByRoleNameAsync(string roleName,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Role>> GetRolesByPermissionIdAsync(PermissionId permissionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);
    void Remove(RolePermission rolePermission);
    Task<bool> ExistsAsync(RoleId roleId, PermissionId permissionId, CancellationToken cancellationToken = default);

    Task<RolePermission?> GetAsync(RoleId roleId, PermissionId permissionId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
