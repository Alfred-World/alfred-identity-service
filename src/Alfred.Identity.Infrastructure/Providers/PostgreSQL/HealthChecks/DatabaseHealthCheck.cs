using System.Data;
using System.Diagnostics;

using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.HealthChecks;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Providers.SqlServer.HealthChecks;

/// <summary>
/// Health check for database connection
/// Validates that the database is accessible and responsive
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbContext _dbContext;

    public string ServiceName => "Database";

    public DatabaseHealthCheck(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Test 1: Open connection
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            // Test 2: Execute simple query
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                stopwatch.Stop();
                return HealthCheckResult.Unhealthy(
                    ServiceName,
                    "Database connection failed - cannot connect to database server",
                    stopwatch.Elapsed);
            }

            // Test 3: Execute query to verify database is responsive
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5; // 5 second timeout

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result == null || (int)result != 1)
            {
                stopwatch.Stop();
                return HealthCheckResult.Degraded(
                    ServiceName,
                    "Database is accessible but query returned unexpected result",
                    stopwatch.Elapsed);
            }

            stopwatch.Stop();

            // Get database provider info
            var providerName = _dbContext.Database.ProviderName ?? "Unknown";
            var databaseName = connection.Database;

            return HealthCheckResult.Healthy(
                ServiceName,
                $"Connected to {providerName} database '{databaseName}'",
                stopwatch.Elapsed);
        }
        catch (TimeoutException ex)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                ServiceName,
                $"Database connection timeout: {ex.Message}",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return HealthCheckResult.Unhealthy(
                ServiceName,
                $"Database connection failed: {ex.Message}",
                stopwatch.Elapsed);
        }
    }
}
