using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds Owner role permission mapping.
/// - Owner: system:* (full access)
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
        var ownerRole = await _dbContext.Set<Role>()
            .FirstOrDefaultAsync(r => r.NormalizedName == "OWNER", cancellationToken);
        if (ownerRole == null)
        {
            LogWarning("Owner role not found. Skipping.");
            return;
        }

        var systemWildcard = await _dbContext.Set<Permission>()
            .FirstOrDefaultAsync(p => p.Code == "system:*", cancellationToken);
        if (systemWildcard == null)
        {
            LogWarning("system:* permission not found. Skipping.");
            return;
        }

        var exists = await _dbContext.Set<RolePermission>()
            .AnyAsync(rp => rp.RoleId == ownerRole.Id && rp.PermissionId == systemWildcard.Id, cancellationToken);
        if (exists)
        {
            LogSuccess("Skipped (Owner -> system:* mapping exists)");
            return;
        }

        await _dbContext.Set<RolePermission>().AddAsync(RolePermission.Create(ownerRole.Id, systemWildcard.Id),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogSuccess("Created Owner -> system:* mapping");
    }
}
