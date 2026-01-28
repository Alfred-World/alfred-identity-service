using System.Text.Json;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;

using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Services;

/// <summary>
/// Service to sync role-permission mappings to cache (Redis).
/// Key format: "permissions:{ROLE_NAME}" -> ["permission1", "permission2", ...]
/// </summary>
public class PermissionCacheService : IPermissionCacheService
{
    private const string CacheKeyPrefix = "permissions:";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(24);

    private readonly ICacheProvider _cacheProvider;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly ILogger<PermissionCacheService> _logger;

    public PermissionCacheService(
        ICacheProvider cacheProvider,
        IRolePermissionRepository rolePermissionRepository,
        ILogger<PermissionCacheService> logger)
    {
        _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
        _rolePermissionRepository = rolePermissionRepository ??
                                    throw new ArgumentNullException(nameof(rolePermissionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SyncRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedRoleName = roleName.ToUpperInvariant();
            var permissions =
                await _rolePermissionRepository.GetPermissionsByRoleNameAsync(roleName, cancellationToken);
            var permissionCodes = permissions.Select(p => p.Code).ToList();

            var cacheKey = $"{CacheKeyPrefix}{normalizedRoleName}";
            var json = JsonSerializer.Serialize(permissionCodes);

            await _cacheProvider.SetAsync(cacheKey, json, DefaultExpiration, cancellationToken);

            _logger.LogInformation(
                "Synced {Count} permissions for role {RoleName} to cache",
                permissionCodes.Count, roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync permissions for role {RoleName} to cache", roleName);
            throw;
        }
    }

    public async Task SyncAllRolePermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all unique roles from role_permissions
            var ownerPermissions =
                await _rolePermissionRepository.GetPermissionsByRoleNameAsync("Owner", cancellationToken);
            var adminPermissions =
                await _rolePermissionRepository.GetPermissionsByRoleNameAsync("Admin", cancellationToken);
            var userPermissions =
                await _rolePermissionRepository.GetPermissionsByRoleNameAsync("User", cancellationToken);

            // Sync each role
            await SyncPermissionsToCache("OWNER", ownerPermissions.Select(p => p.Code).ToList(), cancellationToken);
            await SyncPermissionsToCache("ADMIN", adminPermissions.Select(p => p.Code).ToList(), cancellationToken);
            await SyncPermissionsToCache("USER", userPermissions.Select(p => p.Code).ToList(), cancellationToken);

            _logger.LogInformation("Synced all role permissions to cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync all role permissions to cache");
            throw;
        }
    }

    public async Task InvalidateRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var normalizedRoleName = roleName.ToUpperInvariant();
        var cacheKey = $"{CacheKeyPrefix}{normalizedRoleName}";

        await _cacheProvider.DeleteAsync(cacheKey, cancellationToken);
        _logger.LogInformation("Invalidated cache for role {RoleName}", roleName);
    }

    public async Task<IReadOnlyList<string>> GetRolePermissionsAsync(string roleName,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoleName = roleName.ToUpperInvariant();
        var cacheKey = $"{CacheKeyPrefix}{normalizedRoleName}";

        var json = await _cacheProvider.GetAsync(cacheKey, cancellationToken);
        if (string.IsNullOrEmpty(json))
        {
            // Cache miss - try to sync and return
            _logger.LogDebug("Cache miss for role {RoleName}, syncing from DB", roleName);
            await SyncRolePermissionsAsync(roleName, cancellationToken);

            json = await _cacheProvider.GetAsync(cacheKey, cancellationToken);
            if (string.IsNullOrEmpty(json))
            {
                return Array.Empty<string>();
            }
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize permissions for role {RoleName}", roleName);
            return Array.Empty<string>();
        }
    }

    public async Task<bool> HasPermissionAsync(string roleName, string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetRolePermissionsAsync(roleName, cancellationToken);
        var normalizedCode = permissionCode.ToLowerInvariant();

        // Check for exact match or wildcard (Owner has "*" = all permissions)
        return permissions.Contains(normalizedCode) || permissions.Contains("*");
    }

    private async Task SyncPermissionsToCache(string normalizedRoleName, List<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{normalizedRoleName}";
        var json = JsonSerializer.Serialize(permissionCodes);
        await _cacheProvider.SetAsync(cacheKey, json, DefaultExpiration, cancellationToken);
    }
}
