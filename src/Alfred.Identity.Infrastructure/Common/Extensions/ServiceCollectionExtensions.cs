using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Infrastructure.Common.HealthChecks;
using Alfred.Identity.Infrastructure.Common.Options;
using Alfred.Identity.Infrastructure.Common.Seeding;
using Alfred.Identity.Infrastructure.Providers.Cache;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;
using Alfred.Identity.Infrastructure.Repositories;
using Alfred.Identity.Infrastructure.Services;
using Alfred.Identity.Infrastructure.Services.Security;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IAuthorizationRepository, AuthorizationRepository>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<ISigningKeyRepository, SigningKeyRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Token Services
        services.AddSingleton<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IJwksService, JwksService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Caching
        services.AddScoped<IPermissionCacheService, PermissionCacheService>();
        services.AddInMemoryCache();

        // Location Services
        services.AddHttpClient<IpApiLocationService>();
        services.AddScoped<ILocationService, IpApiLocationService>();

        // Other Services
        services.AddScoped<IAuthorizationCodeService, AuthorizationCodeService>();

        // Orchestrators
        services.AddScoped<HealthCheckOrchestrator>();

        // Data Seeding
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        services.AddScoped(sp => new DataSeederOrchestrator(
            sp,
            sp.GetRequiredService<ILogger<DataSeederOrchestrator>>(),
            environment));

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services)
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

        // Register PostgreSQL database provider
        services.AddPostgreSQL(connectionString);


        return services;
    }
}
