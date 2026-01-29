using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

public class ScopeSeeder : BaseDataSeeder
{
    private readonly IDbContext _dbContext;

    public ScopeSeeder(IDbContext dbContext, ILogger<ScopeSeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
    }

    public override string Name => "20260129161700_ScopeSeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Set<Scope>().AnyAsync(cancellationToken))
        {
            LogSuccess("Skipped (scopes exist)");
            return;
        }

        var scopes = new List<Scope>
        {
            Scope.Create("openid", "OpenID", "Required for OpenID Connect"),
            Scope.Create("profile", "Profile", "Access to user profile information (name, picture, etc.)"),
            Scope.Create("email", "Email", "Access to user email address"),
            Scope.Create("offline_access", "Offline Access", "Required to receive a refresh token"),
            Scope.Create("address", "Address", "Access to user address information"),
            Scope.Create("phone", "Phone Number", "Access to user phone number")
        };

        await _dbContext.Set<Scope>().AddRangeAsync(scopes, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogSuccess($"Created {scopes.Count} standard OIDC scopes");
    }
}
