using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds initial admin user for Alfred Identity Service
/// </summary>
public class AdminUserSeeder : BaseDataSeeder
{
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
        LogInfo("Starting to seed admin user...");

        // Check if users already exist
        if (await _dbContext.Set<User>().AnyAsync(cancellationToken))
        {
            LogInfo("Users already exist, skipping seed");
            return;
        }

        // Default admin password - should be changed after first login
        var defaultPassword = "Admin@123";
        var hashedPassword = _passwordHasher.HashPassword(defaultPassword);

        var admin = User.Create(
            "admin@gmail.com",
            hashedPassword,
            "System Administrator",
            true
        );

        // Set username to 'admin' instead of email
        admin.SetUserName("admin");

        await _dbContext.Set<User>().AddAsync(admin, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Assign Admin role to the user
        var adminRole = await _dbContext.Set<Role>()
            .FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN", cancellationToken);

        if (adminRole != null)
        {
            var userRole = UserRole.Create(admin.Id, adminRole.Id);
            await _dbContext.Set<UserRole>().AddAsync(userRole, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            LogInfo("Assigned Admin role to admin user");
        }

        LogInfo("Seeded admin user: admin / Admin@123 (or admin@gmail.com)");
        LogSuccess();
    }
}
