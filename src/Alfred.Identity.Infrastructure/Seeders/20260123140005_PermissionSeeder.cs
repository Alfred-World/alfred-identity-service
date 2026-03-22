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
        var requiredPermissions = new[]
        {
            // ===== System Permissions (Owner only) =====
            (Code: "system:*", Name: "Full System Access",
                Description: "Complete administrative access to all system functions"),
            (Code: "system:rotate-keys", Name: "Rotate Signing Keys",
                Description: "Rotate authentication signing keys manually"),

            // ===== User Management =====
            (Code: "users:read", Name: "View Users", Description: "View user list and details"),
            (Code: "users:create", Name: "Create Users", Description: "Create new user accounts"),
            (Code: "users:update", Name: "Update Users", Description: "Modify existing user accounts"),
            (Code: "users:delete", Name: "Delete Users", Description: "Remove user accounts"),
            (Code: "users:ban", Name: "Ban Users", Description: "Ban users from accessing the system"),
            (Code: "users:unban", Name: "Unban Users", Description: "Restore access for banned users"),
            (Code: "users:confirm-email", Name: "Confirm User Email",
                Description: "Allow admins to mark a user email as confirmed without token flow"),

            // ===== Role Management =====
            (Code: "roles:read", Name: "View Roles", Description: "View role list and details"),
            (Code: "roles:create", Name: "Create Roles", Description: "Create new roles"),
            (Code: "roles:update", Name: "Update Roles", Description: "Modify existing roles"),
            (Code: "roles:delete", Name: "Delete Roles", Description: "Remove roles"),

            // ===== Permission Management =====
            (Code: "permissions:read", Name: "View Permissions", Description: "View permission list and details"),
            (Code: "permissions:assign", Name: "Assign Permissions", Description: "Assign permissions to roles"),
            (Code: "permissions:revoke", Name: "Revoke Permissions", Description: "Revoke permissions from roles"),

            // ===== OAuth2 Application Management =====
            (Code: "applications:read", Name: "View Applications", Description: "View OAuth2 client applications"),
            (Code: "applications:create", Name: "Create Applications",
                Description: "Register new OAuth2 client applications"),
            (Code: "applications:update", Name: "Update Applications",
                Description: "Modify existing OAuth2 client applications"),
            (Code: "applications:delete", Name: "Delete Applications",
                Description: "Remove OAuth2 client applications"),

            // ===== Audit & Logs =====
            (Code: "audit:read", Name: "View Audit Logs", Description: "View system audit logs and activity history"),

            // ===== Profile (User-facing) =====
            (Code: "profile:read", Name: "View Own Profile", Description: "View own user profile"),
            (Code: "profile:update", Name: "Update Own Profile", Description: "Update own user profile information")
        };

        var existingCodes = await _dbContext.Set<Permission>()
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);
        var existingCodeSet = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingPermissions = requiredPermissions
            .Where(p => !existingCodeSet.Contains(p.Code))
            .Select(p => Permission.Create(p.Code, p.Name, p.Description))
            .ToList();

        if (!missingPermissions.Any())
        {
            LogSuccess("Skipped (all permissions exist)");
            return;
        }

        await _dbContext.Set<Permission>().AddRangeAsync(missingPermissions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogSuccess($"Created {missingPermissions.Count} missing permissions");
    }
}
