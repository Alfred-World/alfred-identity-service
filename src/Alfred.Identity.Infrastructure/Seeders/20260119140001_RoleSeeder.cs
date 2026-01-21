using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Seeding;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds initial roles (Admin, User) for Alfred Identity Service
/// </summary>
public class RoleSeeder : BaseDataSeeder
{
    private readonly PostgreSqlDbContext _dbContext;

    public RoleSeeder(PostgreSqlDbContext dbContext, ILogger<RoleSeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
    }

    public override string Name => "20260119140001_RoleSeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        LogInfo("Starting to seed roles...");

        // Check if roles already exist
        if (await _dbContext.Roles.AnyAsync(cancellationToken))
        {
            LogInfo("Roles already exist, skipping seed");
            return;
        }

        Role[] roles = new[]
        {
            Role.Create("Admin"),
            Role.Create("User")
        };

        await _dbContext.Roles.AddRangeAsync(roles, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogInfo($"Seeded {roles.Length} roles successfully");
        LogSuccess();
    }
}
