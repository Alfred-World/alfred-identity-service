namespace Alfred.Identity.Domain.Abstractions.Services;

/// <summary>
/// Service for syncing role permissions to cache (Redis/InMemory).
/// Key format: "permissions:{roleName}" -> JSON array of permission codes
/// </summary>
public interface IPermissionCacheService
{
    /// <summary>
    /// Sync permissions for a specific role to cache.
    /// Called when permissions for a role are changed.
    /// </summary>
    Task SyncRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync permissions for all roles to cache.
    /// Called on application startup or cache invalidation.
    /// </summary>
    Task SyncAllRolePermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate (delete) cached permissions for a role.
    /// </summary>
    Task InvalidateRoleAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get permissions for a role from cache.
    /// Returns empty list if not found.
    /// </summary>
    Task<IReadOnlyList<string>> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a role has a specific permission (from cache).
    /// </summary>
    Task<bool> HasPermissionAsync(string roleName, string permissionCode, CancellationToken cancellationToken = default);
}
