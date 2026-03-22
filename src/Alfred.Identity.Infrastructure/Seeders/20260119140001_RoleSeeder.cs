using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds only Owner role for Alfred Identity Service.
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
        var ownerRole = await _dbContext.Set<Role>()
            .FirstOrDefaultAsync(r => r.NormalizedName == "OWNER", cancellationToken);

        if (ownerRole != null)
        {
            LogSuccess("Skipped (Owner role exists)");
            return;
        }

        await _dbContext.Set<Role>().AddAsync(Role.CreateOwner(), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogSuccess("Created Owner role");
    }
}
