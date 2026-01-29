using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds initial roles (Admin, User) for Alfred Identity Service
/// </summary>
public class RoleSeeder : BaseDataSeeder
{
    private readonly IDbContext _dbContext;

    public RoleSeeder(IDbContext dbContext, ILogger<RoleSeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
    }

    public override string Name => "20260119140001_RoleSeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if roles already exist
        if (await _dbContext.Set<Role>().AnyAsync(cancellationToken))
        {
            LogSuccess("Skipped (roles exist)");
            return;
        }

        var roles = new[]
        {
            Role.CreateOwner(), // IsImmutable=true, IsSystem=true
            Role.CreateAdmin(), // IsImmutable=false, IsSystem=true
            Role.CreateUser() // IsImmutable=false, IsSystem=true
        };

        LogDebug($"Creating {roles.Length} roles: Owner, Admin, User");

        await _dbContext.Set<Role>().AddRangeAsync(roles, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogSuccess($"Created {roles.Length} roles");
    }
}
