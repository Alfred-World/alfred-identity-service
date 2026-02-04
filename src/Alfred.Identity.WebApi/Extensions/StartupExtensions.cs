using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.HealthChecks;
using Alfred.Identity.Infrastructure.Common.Seeding;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.WebApi.Extensions;

/// <summary>
/// Extension methods for application startup tasks
/// </summary>
public static class StartupExtensions
{
    /// <summary>
    /// Validate all infrastructure services are available before starting the application
    /// </summary>
    public static async Task ValidateServicesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var healthCheckOrchestrator = scope.ServiceProvider.GetRequiredService<HealthCheckOrchestrator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var allHealthy = await healthCheckOrchestrator.ValidateAllServicesAsync();

        if (!allHealthy)
        {
            logger.LogCritical("[FATAL] Application startup failed - required services are unavailable");
            logger.LogCritical("Please check your configuration and ensure all services are running.");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Run database migrations automatically
    /// </summary>
    public static async Task RunDatabaseMigrationsAsync(this WebApplication app, ILogger logger)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var migrations = await context.Database.GetPendingMigrationsAsync();

            if (migrations.Any())
            {
                logger.LogInformation("Found {MigrationCount} pending migration(s)", migrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations completed successfully");
            }
            else
            {
                logger.LogInformation("No pending migrations found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running database migrations");
            throw;
        }
    }

    /// <summary>
    /// Run data seeders (environment-aware - runs different seeders based on environment)
    /// </summary>
    public static async Task RunDataSeedersAsync(this WebApplication app, ILogger logger)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<DataSeederOrchestrator>();
            await orchestrator.SeedAllAsync();
            logger.LogInformation("Data seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running data seeders");
            throw;
        }
    }

    /// <summary>
    /// Run production startup tasks (migrations and seeders)
    /// </summary>
    public static async Task RunProductionStartupTasksAsync(this WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Running database migrations...");
        await app.RunDatabaseMigrationsAsync(logger);

        logger.LogInformation("Running data seeders...");
        await app.RunDataSeedersAsync(logger);
    }
}
