using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Email;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Options;
using Alfred.Identity.Infrastructure.Repositories;

using Microsoft.Extensions.DependencyInjection;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL;

/// <summary>
/// Extension methods for configuring PostgreSQL provider
/// Supports provider-agnostic repository pattern
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSQL(this IServiceCollection services, string connectionString)
    {
        PostgreSqlOptions options = new() { ConnectionString = connectionString };
        return AddPostgreSQL(services, options);
    }

    public static IServiceCollection AddPostgreSQL(this IServiceCollection services, PostgreSqlOptions options)
    {
        // Register DbContext Factory
        services.AddScoped<IDbContextFactory, PostgreSqlDbContextFactory>(_ => new PostgreSqlDbContextFactory(options));

        // Register DbContext as IDbContext (scoped)
        services.AddScoped<IDbContext>(provider =>
            provider.GetRequiredService<IDbContextFactory>().CreateContext());

        // Register PostgreSqlDbContext for backward compatibility (if needed)
        services.AddScoped(_ => new PostgreSqlDbContext(options));

        // Register System repositories
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();

        // Register Identity repositories (will be added as you develop)
        // Example:
        // services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Register Unit of Work (database-agnostic)
        services.AddScoped<IUnitOfWork, DefaultUnitOfWork>();

        // Register Data Seeders here as needed
        // Example: services.AddScoped<IDataSeeder, InitialDataSeeder>();

        // Register Seed History Repository (if needed)
        // services.AddScoped<ISeedHistoryRepository, SeedHistoryRepository>();

        return services;
    }
}
