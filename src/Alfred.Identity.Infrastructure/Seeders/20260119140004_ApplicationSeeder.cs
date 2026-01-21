using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Seeding;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds default OAuth2/OIDC applications for Alfred Identity Service
/// </summary>
public class ApplicationSeeder : BaseDataSeeder
{
    private readonly PostgreSqlDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public ApplicationSeeder(
        PostgreSqlDbContext dbContext, 
        IPasswordHasher passwordHasher,
        ILogger<ApplicationSeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public override string Name => "20260119140004_ApplicationSeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        LogInfo("Starting to seed applications...");

        // Check if applications already exist
        if (await _dbContext.Applications.AnyAsync(cancellationToken))
        {
            LogInfo("Applications already exist, skipping seed");
            return;
        }

        // Default client secret for development
        const string defaultClientSecret = "alfred-identity-client-secret-2026";
        string hashedSecret = _passwordHasher.HashPassword(defaultClientSecret);

        var applications = new[]
        {
            // Identity Web Client - for SSO authentication UI
            Application.Create(
                clientId: "alfred-identity-web",
                displayName: "Alfred Identity Web",
                clientSecret: hashedSecret,
                redirectUris: "[\"http://localhost:7100/callback\",\"http://identity.test:7100/callback\",\"https://identity.alfred.com/callback\"]",
                postLogoutRedirectUris: "[\"http://localhost:7100\",\"http://identity.test:7100\",\"https://identity.alfred.com\"]",
                permissions: "[\"ept:authorization\",\"ept:token\",\"ept:userinfo\",\"gt:authorization_code\",\"gt:refresh_token\",\"scp:openid\",\"scp:profile\",\"scp:email\"]",
                clientType: "confidential",
                applicationType: "web"
            ),
            
            // Core API Client - backend service
            Application.Create(
                clientId: "alfred-core-api",
                displayName: "Alfred Core API",
                clientSecret: hashedSecret,
                redirectUris: "[\"http://localhost:5001/callback\",\"http://api.test:5001/callback\",\"https://api.alfred.com/callback\"]",
                postLogoutRedirectUris: "[\"http://localhost:5001\",\"http://api.test:5001\",\"https://api.alfred.com\"]",
                permissions: "[\"ept:authorization\",\"ept:token\",\"ept:userinfo\",\"gt:authorization_code\",\"gt:refresh_token\",\"gt:client_credentials\",\"scp:openid\",\"scp:profile\",\"scp:email\",\"scp:api\"]",
                clientType: "confidential",
                applicationType: "web"
            ),

            // Gateway Client - API Gateway
            Application.Create(
                clientId: "alfred-gateway",
                displayName: "Alfred Gateway",
                clientSecret: hashedSecret,
                redirectUris: "[\"http://localhost:8080/callback\",\"http://gateway.test:8080/callback\",\"https://gateway.alfred.com/callback\"]",
                postLogoutRedirectUris: "[\"http://localhost:8080\",\"http://gateway.test:8080\",\"https://gateway.alfred.com\"]",
                permissions: "[\"ept:authorization\",\"ept:token\",\"ept:introspection\",\"gt:client_credentials\",\"scp:api\"]",
                clientType: "confidential",
                applicationType: "web"
            ),

            // Core Web Client - SPA with PKCE (Public Client)
            Application.Create(
                clientId: "core_web",
                displayName: "Alfred Core Web",
                clientSecret: null, // Public client - NO secret required
                redirectUris: "[\"https://core.test/callback\",\"http://core.test:7200/callback\",\"http://localhost:7200/callback\"]",
                postLogoutRedirectUris: "[\"https://core.test\",\"http://core.test:7200\",\"http://localhost:7200\"]",
                permissions: "[\"ept:authorization\",\"ept:token\",\"ept:userinfo\",\"gt:authorization_code\",\"gt:refresh_token\",\"scp:openid\",\"scp:profile\",\"scp:email\",\"scp:offline_access\"]",
                clientType: "public", // PUBLIC client for SPA - PKCE required
                applicationType: "web"
            ),

            // SSO Web Client - SPA with PKCE (Public Client for profile/dashboard)
            Application.Create(
                clientId: "sso_web",
                displayName: "Alfred SSO Web",
                clientSecret: null, // Public client - NO secret required
                redirectUris: "[\"https://sso.test/callback\",\"http://sso.test:7100/callback\",\"http://localhost:7100/callback\"]",
                postLogoutRedirectUris: "[\"https://sso.test\",\"http://sso.test:7100\",\"http://localhost:7100\"]",
                permissions: "[\"ept:authorization\",\"ept:token\",\"ept:userinfo\",\"gt:authorization_code\",\"gt:refresh_token\",\"scp:openid\",\"scp:profile\",\"scp:email\",\"scp:offline_access\"]",
                clientType: "public", // PUBLIC client for SPA - PKCE required
                applicationType: "web"
            ),
        };

        await _dbContext.Applications.AddRangeAsync(applications, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogInfo($"Seeded {applications.Length} applications successfully");
        LogInfo($"Default client secret for development: {defaultClientSecret}");
        LogSuccess();
    }
}
