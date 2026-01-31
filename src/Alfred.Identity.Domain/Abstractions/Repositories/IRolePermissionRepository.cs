using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

/// <summary>
/// Repository interface for RolePermission entity (many-to-many join table)
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>
    /// Get all permissions for a specific role
    /// </summary>
    Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all permissions for a role by role name
    /// </summary>
    Task<IEnumerable<Permission>> GetPermissionsByRoleNameAsync(string roleName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all roles that have a specific permission
    /// </summary>
    Task<IEnumerable<Role>> GetRolesByPermissionIdAsync(Guid permissionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign a permission to a role
    /// </summary>
    Task AddAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a permission from a role
    /// </summary>
    void Remove(RolePermission rolePermission);

    /// <summary>
    /// Check if a role has a specific permission
    /// </summary>
    Task<bool> ExistsAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific role-permission mapping
    /// </summary>
    Task<RolePermission?> GetAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
