using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds initial permissions for Alfred Identity Service.
/// Permissions follow the pattern: "resource:action" (e.g., "users:read", "applications:delete")
/// </summary>
public class PermissionSeeder : BaseDataSeeder
{
    private readonly IDbContext _dbContext;

    public PermissionSeeder(IDbContext dbContext, ILogger<PermissionSeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
    }

    public override string Name => "20260123140005_PermissionSeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if permissions already exist
        if (await _dbContext.Set<Permission>().AnyAsync(cancellationToken))
        {
            LogSuccess("Skipped (permissions exist)");
            return;
        }

        var permissions = new List<Permission>
        {
            // ===== System Permissions (Owner only) =====
            Permission.Create("system:*", "Full System Access",
                "Complete administrative access to all system functions"),

            // ===== User Management =====
            Permission.Create("users:read", "View Users", "View user list and details"),
            Permission.Create("users:create", "Create Users", "Create new user accounts"),
            Permission.Create("users:update", "Update Users", "Modify existing user accounts"),
            Permission.Create("users:delete", "Delete Users", "Remove user accounts"),

            // ===== Role Management =====
            Permission.Create("roles:read", "View Roles", "View role list and details"),
            Permission.Create("roles:create", "Create Roles", "Create new roles"),
            Permission.Create("roles:update", "Update Roles", "Modify existing roles"),
            Permission.Create("roles:delete", "Delete Roles", "Remove roles"),

            // ===== Permission Management =====
            Permission.Create("permissions:read", "View Permissions", "View permission list and details"),
            Permission.Create("permissions:assign", "Assign Permissions", "Assign permissions to roles"),
            Permission.Create("permissions:revoke", "Revoke Permissions", "Revoke permissions from roles"),

            // ===== OAuth2 Application Management =====
            Permission.Create("applications:read", "View Applications", "View OAuth2 client applications"),
            Permission.Create("applications:create", "Create Applications", "Register new OAuth2 client applications"),
            Permission.Create("applications:update", "Update Applications",
                "Modify existing OAuth2 client applications"),
            Permission.Create("applications:delete", "Delete Applications", "Remove OAuth2 client applications"),

            // ===== Audit & Logs =====
            Permission.Create("audit:read", "View Audit Logs", "View system audit logs and activity history"),

            // ===== Profile (User-facing) =====
            Permission.Create("profile:read", "View Own Profile", "View own user profile"),
            Permission.Create("profile:update", "Update Own Profile", "Update own user profile information")
        };

        await _dbContext.Set<Permission>().AddRangeAsync(permissions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogSuccess($"Created {permissions.Count} permissions");
    }
}
