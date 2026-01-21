using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Seeding;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds initial default user for Alfred Identity Service
/// </summary>
public class DefaultUserSeeder : BaseDataSeeder
{
    private readonly PostgreSqlDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public DefaultUserSeeder(
        PostgreSqlDbContext dbContext, 
        IPasswordHasher passwordHasher,
        ILogger<DefaultUserSeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public override string Name => "20260119140003_DefaultUserSeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        LogInfo("Starting to seed default user...");

        // Check if default user already exists
        var defaultEmail = "user@gmail.com";
        if (await _dbContext.Users.AnyAsync(u => u.Email == defaultEmail, cancellationToken))
        {
            LogInfo("Default user already exists, skipping seed");
            return;
        }

        // Default user password - should be changed after first login
        string defaultPassword = "Admin@123";
        string hashedPassword = _passwordHasher.HashPassword(defaultPassword);

        User user = User.Create(
            email: defaultEmail,
            passwordHash: hashedPassword,
            fullName: "Default User",
            emailConfirmed: true
        );
        user.SetUserName("user");

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Assign User role to the user
        var userRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == "USER", cancellationToken);

        if (userRole != null)
        {
            var roleAssignment = UserRole.Create(user.Id, userRole.Id);
            await _dbContext.UserRoles.AddAsync(roleAssignment, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            LogInfo("Assigned User role to default user");
        }

        LogInfo($"Seeded default user: {defaultEmail} (Password: {defaultPassword})");
        LogSuccess();
    }
}
