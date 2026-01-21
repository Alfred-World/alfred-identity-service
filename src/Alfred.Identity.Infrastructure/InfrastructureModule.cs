using Alfred.Identity.Infrastructure.Common.HealthChecks;
using Alfred.Identity.Infrastructure.Common.Options;
using Alfred.Identity.Infrastructure.Common.Seeding;
using Alfred.Identity.Infrastructure.Providers.Cache;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure;

/// <summary>
/// Infrastructure module - configures the appropriate database provider
/// </summary>
public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Get database provider - REQUIRED, no default
        var providerStr = Environment.GetEnvironmentVariable("DB_PROVIDER");
        if (string.IsNullOrEmpty(providerStr))
        {
            throw new InvalidOperationException(
                "DB_PROVIDER environment variable is required. Valid value: 'PostgreSQL'");
        }

        // Validate provider
        if (!Enum.TryParse<DatabaseProvider>(providerStr, true, out var provider))
        {
            throw new InvalidOperationException(
                $"Invalid DB_PROVIDER value: '{providerStr}'. Valid value: 'PostgreSQL'");
        }

        // Get database connection parameters
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "alfred_identity";
        var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";

        // Build connection string based on provider
        string connectionString;
        switch (provider)
        {
            case DatabaseProvider.PostgreSQL:
                connectionString =
                    $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};";
                break;

            default:
                throw new InvalidOperationException($"Unsupported database provider: {provider}");
        }

        // Register Data Seeder Orchestrator with environment awareness
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        services.AddScoped(sp => new DataSeederOrchestrator(
            sp,
            sp.GetRequiredService<ILogger<DataSeederOrchestrator>>(),
            environment));

        // Register Health Check Orchestrator
        services.AddScoped<HealthCheckOrchestrator>();

        // Register PostgreSQL database provider
        services.AddPostgreSQL(connectionString);

        // Register Cache Provider (In-Memory for now)
        services.AddInMemoryCache();

        // Register AuthTokenService for Token Exchange Pattern
        services.AddSingleton<Domain.Abstractions.IAuthTokenService, Services.AuthTokenService>();

        return services;
    }
}
