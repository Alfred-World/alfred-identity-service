using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds single owner user for Alfred Identity Service.
/// </summary>
public class AdminUserSeeder : BaseDataSeeder
{
    private static readonly UserId HardcodedOwnerUserId = new(Guid.Parse("019d046f-2094-7e64-8caa-715ad7272a34"));
    private readonly IDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public AdminUserSeeder(
        IDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<AdminUserSeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public override string Name => "20260119140002_AdminUserSeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var ownerRole = await _dbContext.Set<Role>()
            .FirstOrDefaultAsync(r => r.NormalizedName == "OWNER", cancellationToken);

        if (ownerRole == null)
        {
            LogWarning("Owner role not found. Skipping owner user seed.");
            return;
        }

        var defaultPassword = "Admin@123";
        var hashedPassword = _passwordHasher.HashPassword(defaultPassword);

        var owner = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == HardcodedOwnerUserId, cancellationToken);

        if (owner == null)
        {
            owner = User.CreateWithId(
                HardcodedOwnerUserId,
                "owner@gmail.com",
                "owner",
                hashedPassword,
                "System Owner",
                true);

            await _dbContext.Set<User>().AddAsync(owner, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            LogDebug("Created owner user with hardcoded id");
        }
        else
        {
            LogDebug("Owner user already exists, skipping user creation");
        }

        var hasOwnerRole = await _dbContext.Set<UserRole>()
            .AnyAsync(ur => ur.UserId == HardcodedOwnerUserId && ur.RoleId == ownerRole.Id, cancellationToken);

        if (!hasOwnerRole)
        {
            var userRole = UserRole.Create(HardcodedOwnerUserId, ownerRole.Id);
            await _dbContext.Set<UserRole>().AddAsync(userRole, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            LogDebug("Assigned Owner role");
        }

        LogSuccess("Ensured owner user (owner / Admin@123) and Owner role assignment");
    }
}
