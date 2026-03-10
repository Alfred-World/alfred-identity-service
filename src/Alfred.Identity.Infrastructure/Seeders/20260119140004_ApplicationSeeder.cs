using System.Text.Json;

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
    private readonly IClientSecretHasher _clientSecretHasher;

    public ApplicationSeeder(
        IDbContext dbContext,
        IClientSecretHasher clientSecretHasher,
        ILogger<ApplicationSeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
        _clientSecretHasher = clientSecretHasher;
    }

    public override string Name => "20260119140004_ApplicationSeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Default client secret for development
        const string defaultClientSecret = "alfred-client-secret-2026";
        var hashedSecret = _clientSecretHasher.HashSecret(defaultClientSecret);

        // Read URLs from environment (required — no silent defaults)
        var coreWebUrl = GetRequiredEnv("URLS_CORE_WEB", "Urls__CoreWeb");
        var ssoWebUrl = GetRequiredEnv("URLS_SSO_WEB", "Urls__SsoWeb");

        // Build redirect URI lists to include both prod domain and dev defaults
        var coreRedirectUris = BuildUniqueJsonArray(new[]
        {
            $"{coreWebUrl}/api/auth/callback/alfred-identity",
            "https://core.test/api/auth/callback/alfred-identity",
            "http://core.test:7200/api/auth/callback/alfred-identity",
            "http://localhost:7200/api/auth/callback/alfred-identity"
        });

        var corePostLogoutUris = BuildUniqueJsonArray(new[]
        {
            coreWebUrl,
            $"{coreWebUrl}/login",
            $"{ssoWebUrl}/api/auth/signout",
            $"{ssoWebUrl}/api/auth/logout",
            $"{ssoWebUrl}/api/auth/force-logout",
            $"{ssoWebUrl}/signout",
            $"{ssoWebUrl}/login",
            "https://core.test",
            "https://core.test/login",
            "https://sso.test/api/auth/signout",
            "https://sso.test/api/auth/logout",
            "https://sso.test/signout",
            "https://sso.test/login",
            "http://core.test:7200",
            "http://core.test:7200/login",
            "http://localhost:7200",
            "http://localhost:7200/login"
        });

        var ssoRedirectUris = BuildUniqueJsonArray(new[]
        {
            $"{ssoWebUrl}/callback",
            $"{ssoWebUrl}/api/auth/callback/sso-oauth",
            "https://sso.test/callback",
            "https://sso.test/api/auth/callback/sso-oauth",
            "http://sso.test:7100/callback",
            "http://sso.test:7100/api/auth/callback/sso-oauth",
            "http://localhost:7100/callback",
            "http://localhost:7100/api/auth/callback/sso-oauth"
        });

        var ssoPostLogoutUris = BuildUniqueJsonArray(new[]
        {
            ssoWebUrl,
            $"{ssoWebUrl}/login",
            "https://sso.test",
            "https://sso.test/login",
            "http://sso.test:7100",
            "http://sso.test:7100/login",
            "http://localhost:7100",
            "http://localhost:7100/login"
        });

        var applications = new[]
        {
            // Core Web Client - Next.js App (Confidential Client with NextAuth)
            Application.Create(
                "core_web",
                "Alfred Core Web",
                hashedSecret, // Confidential client - Secret required for NextAuth
                coreRedirectUris,
                corePostLogoutUris,
                "[\"ept:authorization\",\"ept:token\",\"ept:userinfo\",\"gt:authorization_code\",\"gt:refresh_token\",\"scp:openid\",\"scp:profile\",\"scp:email\",\"scp:offline_access\"]",
                "confidential", // CONFIDENTIAL client for NextAuth (Backend-for-Frontend)
                "web"
            ),

            // SSO Web Client - Next.js App (Confidential Client with NextAuth for SSO)
            Application.Create(
                "sso_web",
                "Alfred SSO Web",
                hashedSecret, // Confidential client - Secret required for NextAuth OAuth
                ssoRedirectUris,
                ssoPostLogoutUris,
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

    /// <summary>
    /// Builds a JSON array string from a list of URIs, deduplicating by case-insensitive comparison.
    /// </summary>
    private static string BuildUniqueJsonArray(IEnumerable<string> uris)
    {
        var unique = uris
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return JsonSerializer.Serialize(unique);
    }

    /// <summary>
    /// Reads a required env var, trying primary key first, then legacy docker-compose key.
    /// Fails fast with an actionable error message.
    /// </summary>
    private static string GetRequiredEnv(string primaryKey, string legacyKey)
    {
        var value = Environment.GetEnvironmentVariable(primaryKey)
                    ?? Environment.GetEnvironmentVariable(legacyKey);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"❌ Required environment variable '{primaryKey}' (or '{legacyKey}') is not set. " +
                $"Set it in .env.prod or docker-compose environment.");
        }

        return value.TrimEnd('/');
    }
}
