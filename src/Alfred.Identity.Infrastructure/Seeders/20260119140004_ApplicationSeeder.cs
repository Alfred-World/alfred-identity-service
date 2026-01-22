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

        // Default client secret for development
        const string defaultClientSecret = "alfred-identity-client-secret-2026";
        var hashedSecret = _passwordHasher.HashPassword(defaultClientSecret);

        var applications = new[]
        {
            // Core Web Client - Next.js App (Confidential Client with NextAuth)
            Application.Create(
                clientId: "core_web",
                displayName: "Alfred Core Web",
                clientSecret: hashedSecret, // Confidential client - Secret required for NextAuth
                
                // IMPORTANT: Added NextAuth Callback URL here
                redirectUris: "[\"https://core.test/api/auth/callback/alfred-identity\",\"http://core.test:7200/api/auth/callback/alfred-identity\",\"http://localhost:7200/api/auth/callback/alfred-identity\"]",
                
                postLogoutRedirectUris: "[\"https://core.test\",\"http://core.test:7200\",\"http://localhost:7200\"]",
                permissions: "[\"ept:authorization\",\"ept:token\",\"ept:userinfo\",\"gt:authorization_code\",\"gt:refresh_token\",\"scp:openid\",\"scp:profile\",\"scp:email\",\"scp:offline_access\"]",
                clientType: "confidential", // CONFIDENTIAL client for NextAuth (Backend-for-Frontend)
                applicationType: "web"
            ),

            // SSO Web Client - Next.js App (Confidential Client with NextAuth for SSO)
            Application.Create(
                clientId: "sso_web",
                displayName: "Alfred SSO Web",
                clientSecret: hashedSecret, // Confidential client - Secret required for NextAuth OAuth
                
                // IMPORTANT: Added NextAuth Callback URL for SSO OAuth flow
                redirectUris: "[\"https://sso.test/callback\",\"https://sso.test/api/auth/callback/sso-oauth\",\"http://sso.test:7100/callback\",\"http://sso.test:7100/api/auth/callback/sso-oauth\",\"http://localhost:7100/callback\",\"http://localhost:7100/api/auth/callback/sso-oauth\"]",
                
                postLogoutRedirectUris: "[\"https://sso.test\",\"http://sso.test:7100\",\"http://localhost:7100\"]",
                permissions: "[\"ept:authorization\",\"ept:token\",\"ept:userinfo\",\"gt:authorization_code\",\"gt:refresh_token\",\"scp:openid\",\"scp:profile\",\"scp:email\",\"scp:offline_access\"]",
                clientType: "confidential", // CONFIDENTIAL client for NextAuth (Backend-for-Frontend)
                applicationType: "web"
            )
        };

        foreach (var app in applications)
        {
            var existingApp = await _dbContext.Applications.FirstOrDefaultAsync(a => a.ClientId == app.ClientId, cancellationToken);
            if (existingApp != null)
            {
                // Update existing app
                existingApp.Update(
                    app.DisplayName,
                    app.RedirectUris,
                    app.PostLogoutRedirectUris,
                    app.Permissions,
                    app.ClientType,
                    app.ClientSecret
                );
                LogInfo($"Updated application: {app.ClientId}");
            }
            else
            {
                // Add new app
                await _dbContext.Applications.AddAsync(app, cancellationToken);
                LogInfo($"Added application: {app.ClientId}");
            }
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogInfo($"Seeded {applications.Length} applications successfully");
        LogInfo($"Default client secret for development: {defaultClientSecret}");
        LogSuccess();
    }
}
