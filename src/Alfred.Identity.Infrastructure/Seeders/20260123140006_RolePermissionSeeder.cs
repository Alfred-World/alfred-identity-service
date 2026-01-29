using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds role-permission mappings.
/// - Owner: system:* (full access)
/// - Admin: all management permissions except system:*
/// - User: profile permissions only
/// </summary>
public class RolePermissionSeeder : BaseDataSeeder
{
    private readonly IDbContext _dbContext;

    public RolePermissionSeeder(IDbContext dbContext, ILogger<RolePermissionSeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
    }

    public override string Name => "20260123140006_RolePermissionSeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if role permissions already exist
        if (await _dbContext.Set<RolePermission>().AnyAsync(cancellationToken))
        {
            LogSuccess("Skipped (mappings exist)");
            return;
        }

        // Get roles
        var ownerRole = await _dbContext.Set<Role>().FirstOrDefaultAsync(r => r.Name == "Owner", cancellationToken);
        var adminRole = await _dbContext.Set<Role>().FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);
        var userRole = await _dbContext.Set<Role>().FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);

        if (ownerRole == null || adminRole == null || userRole == null)
        {
            LogWarning("Required roles not found. Skipping.");
            return;
        }

        // Get all permissions
        var allPermissions = await _dbContext.Set<Permission>().ToListAsync(cancellationToken);
        if (!allPermissions.Any())
        {
            LogWarning("No permissions found. Skipping.");
            return;
        }

        var rolePermissions = new List<RolePermission>();

        // ===== Owner Role: Gets system:* only (implies full access) =====
        var systemWildcard = allPermissions.FirstOrDefault(p => p.Code == "system:*");
        if (systemWildcard != null)
        {
            rolePermissions.Add(RolePermission.Create(ownerRole.Id, systemWildcard.Id));
        }

        // ===== Admin Role: Gets all permissions EXCEPT system:* =====
        var adminPermissions = allPermissions.Where(p => p.Code != "system:*");
        foreach (var permission in adminPermissions)
        {
            rolePermissions.Add(RolePermission.Create(adminRole.Id, permission.Id));
        }

        // ===== User Role: Gets profile permissions only =====
        var userPermissions = allPermissions.Where(p => p.Resource == "profile");
        foreach (var permission in userPermissions)
        {
            rolePermissions.Add(RolePermission.Create(userRole.Id, permission.Id));
        }

        await _dbContext.Set<RolePermission>().AddRangeAsync(rolePermissions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogSuccess(
            $"Created {rolePermissions.Count} mappings (Owner:1, Admin:{adminPermissions.Count()}, User:{userPermissions.Count()})");
    }
}
