using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

/// <summary>
/// Repository interface for Permission entity
/// </summary>
public interface IPermissionRepository : IRepository<Permission>
{
    /// <summary>
    /// Get permission by its unique code
    /// </summary>
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all permissions for a specific resource (e.g., "finance")
    /// </summary>
    Task<IEnumerable<Permission>> GetByResourceAsync(string resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active permissions
    /// </summary>
    Task<IEnumerable<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a permission code already exists
    /// </summary>
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);


}
