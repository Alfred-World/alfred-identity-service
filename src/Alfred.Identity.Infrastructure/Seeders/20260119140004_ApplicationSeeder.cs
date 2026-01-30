using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds default OAuth2/OIDC applications for Alfred Identity Service
/// </summary>
public class ApplicationSeeder : BaseDataSeeder
{
    private readonly IDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public ApplicationSeeder(
        IDbContext dbContext,
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
        // Default client secret for development
        const string defaultClientSecret = "alfred-identity-client-secret-2026";
        var hashedSecret = _passwordHasher.HashPassword(defaultClientSecret);

        var applications = new[]
        {
            // Core Web Client - Next.js App (Confidential Client with NextAuth)
            Application.Create(
                "core_web",
                "Alfred Core Web",
                hashedSecret, // Confidential client - Secret required for NextAuth

                // IMPORTANT: Added NextAuth Callback URL here
                "[\"https://core.test/api/auth/callback/alfred-identity\",\"http://core.test:7200/api/auth/callback/alfred-identity\",\"http://localhost:7200/api/auth/callback/alfred-identity\"]",
                "[\"https://core.test\",\"https://core.test/login\",\"https://sso.test/api/auth/signout\",\"https://sso.test/api/auth/logout\",\"https://sso.test/api/auth/force-logout\",\"https://sso.test/signout\",\"https://sso.test/login\",\"http://core.test:7200\",\"http://core.test:7200/login\",\"http://localhost:7200\",\"http://localhost:7200/login\"]",
                "[\"ept:authorization\",\"ept:token\",\"ept:userinfo\",\"gt:authorization_code\",\"gt:refresh_token\",\"scp:openid\",\"scp:profile\",\"scp:email\",\"scp:offline_access\"]",
                "confidential", // CONFIDENTIAL client for NextAuth (Backend-for-Frontend)
                "web"
            ),

            // SSO Web Client - Next.js App (Confidential Client with NextAuth for SSO)
            Application.Create(
                "sso_web",
                "Alfred SSO Web",
                hashedSecret, // Confidential client - Secret required for NextAuth OAuth

                // IMPORTANT: Added NextAuth Callback URL for SSO OAuth flow
                "[\"https://sso.test/callback\",\"https://sso.test/api/auth/callback/sso-oauth\",\"http://sso.test:7100/callback\",\"http://sso.test:7100/api/auth/callback/sso-oauth\",\"http://localhost:7100/callback\",\"http://localhost:7100/api/auth/callback/sso-oauth\"]",
                "[\"https://sso.test\",\"https://sso.test/login\",\"http://sso.test:7100\",\"http://sso.test:7100/login\",\"http://localhost:7100\",\"http://localhost:7100/login\"]",
                "[\"ept:authorization\",\"ept:token\",\"ept:userinfo\",\"gt:authorization_code\",\"gt:refresh_token\",\"scp:openid\",\"scp:profile\",\"scp:email\",\"scp:offline_access\"]",
                "confidential", // CONFIDENTIAL client for NextAuth (Backend-for-Frontend)
                "web"
            )
        };

        var added = 0;
        var updated = 0;

        foreach (var app in applications)
        {
            var existingApp = await _dbContext.Set<Application>()
                .FirstOrDefaultAsync(a => a.ClientId == app.ClientId, cancellationToken);
            if (existingApp != null)
            {
                // Update existing app
                existingApp.Update(
                    app.DisplayName,
                    app.RedirectUris,
                    app.PostLogoutRedirectUris,
                    app.Permissions,
                    app.ClientType
                );

                if (!string.IsNullOrEmpty(app.ClientSecret) && app.ClientSecret != existingApp.ClientSecret)
                {
                    existingApp.RotateSecret(app.ClientSecret);
                }

                LogDebug($"Updated: {app.ClientId}");
                updated++;
            }
            else
            {
                // Add new app
                await _dbContext.Set<Application>().AddAsync(app, cancellationToken);
                LogDebug($"Added: {app.ClientId}");
                added++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var summary = added > 0 && updated > 0
            ? $"Added {added}, updated {updated} apps"
            : added > 0
                ? $"Added {added} apps"
                : $"Updated {updated} apps";
        LogSuccess(summary);
    }
}
