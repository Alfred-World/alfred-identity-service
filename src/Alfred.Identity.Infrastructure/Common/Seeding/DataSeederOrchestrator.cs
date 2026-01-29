using System.Diagnostics;

using Alfred.Identity.Infrastructure.Common.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Npgsql;

namespace Alfred.Identity.Infrastructure.Common.Seeding;

/// <summary>
/// Orchestrates the execution of all data seeders in the correct order.
/// Seeders are sorted alphabetically by Name (use timestamp prefix for ordering).
/// </summary>
public class DataSeederOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSeederOrchestrator> _logger;
    private readonly string _environment;

    public DataSeederOrchestrator(IServiceProvider serviceProvider, ILogger<DataSeederOrchestrator> logger,
        string? environment = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }

    /// <summary>
    /// Execute all registered data seeders in order (sorted by Name)
    /// Only executes seeders appropriate for the current environment
    /// </summary>
    public async Task SeedAllAsync(bool forceReseed = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Starting Data Seeding (Environment: {Environment}) ===", _environment);

        var seeders = _serviceProvider.GetServices<IDataSeeder>()
            .Where(s => ShouldExecuteSeeder(s))
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .ToList();

        if (!seeders.Any())
        {
            _logger.LogWarning("No data seeders registered for the current environment");
            return;
        }

        // Get seed history repository
        var historyRepo = _serviceProvider.GetService<ISeedHistoryRepository>();

        var executedCount = 0;
        var skippedCount = 0;

        foreach (var seeder in seeders)
        {
            // Check if already executed
            if (historyRepo != null && !forceReseed)
            {
                var alreadyExecuted = await historyRepo.HasBeenExecutedAsync(seeder.Name, cancellationToken);
                if (alreadyExecuted)
                {
                    skippedCount++;
                    continue;
                }
            }

            var stopwatch = Stopwatch.StartNew();
            var success = false;
            string? errorMessage = null;

            try
            {
                await seeder.SeedAsync(cancellationToken);
                stopwatch.Stop();
                success = true;
                executedCount++;
                _logger.LogInformation("✓ {SeederName}", seeder.Name);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                errorMessage = ex.Message;
                _logger.LogError(ex, "✗ {SeederName}", seeder.Name);

                // Record failure
                if (historyRepo != null)
                {
                    await historyRepo.RecordExecutionAsync(new SeedHistory
                    {
                        SeederName = seeder.Name,
                        ExecutedAt = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = errorMessage,
                        Duration = stopwatch.Elapsed
                    }, cancellationToken);
                }

                throw;
            }

            // Record success
            if (historyRepo != null && success)
            {
                await historyRepo.RecordExecutionAsync(new SeedHistory
                {
                    SeederName = seeder.Name,
                    ExecutedAt = DateTime.UtcNow,
                    Success = true,
                    Duration = stopwatch.Elapsed
                }, cancellationToken);
            }
        }

        if (executedCount > 0)
        {
            _logger.LogInformation("=== Seeding Completed: {Executed} executed, {Skipped} skipped ===",
                executedCount, skippedCount);
        }
        else
        {
            _logger.LogInformation("=== Seeding Completed: All seeders already executed ===");
        }
    }

    /// <summary>
    /// Get seed execution history
    /// </summary>
    public async Task<List<SeedHistory>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var historyRepo = _serviceProvider.GetService<ISeedHistoryRepository>();
        if (historyRepo == null)
        {
            _logger.LogWarning("Seed history tracking is not configured");
            return new List<SeedHistory>();
        }

        return await historyRepo.GetAllHistoryAsync(cancellationToken);
    }

    /// <summary>
    /// Resync database: Delete all data and reset seed history so IDs restart from 1
    /// </summary>
    public async Task ResyncDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Starting Database Resync (Delete All Data) ===");

        var dbContext = _serviceProvider.GetService<IDbContext>();
        if (dbContext == null)
        {
            _logger.LogError("IDbContext not found in service provider");
            throw new InvalidOperationException("Database context is not configured");
        }

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            if (connection is NpgsqlConnection npgsqlConnection)
            {
                await npgsqlConnection.OpenAsync(cancellationToken);

                // Get all tables in the database (exclude system tables and seed history)
                using var command = npgsqlConnection.CreateCommand();
                command.CommandText = @"
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_type = 'BASE TABLE' 
                    AND table_name NOT IN ('__seed_history', '__ef_migrations_history')
                    AND table_schema = 'public'
                    ORDER BY table_name DESC";

                List<string> tables = new();
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        tables.Add(reader.GetString(0));
                    }
                }

                // Delete all data from tables and reset sequences
                var clearedCount = 0;
                var failedTables = new List<string>();

                foreach (var table in tables)
                {
                    using var deleteCommand = npgsqlConnection.CreateCommand();
                    deleteCommand.CommandText = $@"
                        TRUNCATE TABLE ""{table}"" RESTART IDENTITY CASCADE;";
                    try
                    {
                        await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
                        clearedCount++;
                    }
                    catch (Exception ex)
                    {
                        failedTables.Add($"{table}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Cleared {ClearedCount}/{TotalCount} tables", clearedCount, tables.Count);
                foreach (var failed in failedTables)
                {
                    _logger.LogWarning("  ⚠ Failed: {Table}", failed);
                }

                // Clear seed history to force re-run all seeders and reset ID back to 1
                using (var clearHistoryCommand = npgsqlConnection.CreateCommand())
                {
                    clearHistoryCommand.CommandText = @"
                        DO $$
                        BEGIN
                            IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = '__seed_history') THEN
                                TRUNCATE TABLE ""__seed_history"" RESTART IDENTITY CASCADE;
                            END IF;
                        END $$;";
                    await clearHistoryCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await npgsqlConnection.CloseAsync();
            }
            else
            {
                _logger.LogError("Database connection is not a PostgreSQL connection");
                throw new InvalidOperationException("Invalid database connection type");
            }

            _logger.LogInformation("=== Database Resync Complete ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database resync");
            throw;
        }
    }

    /// <summary>
    /// Check if a seeder should be executed in the current environment
    /// </summary>
    private bool ShouldExecuteSeeder(IDataSeeder seeder)
    {
        // If seeder doesn't implement IEnvironmentAwareSeeder, execute it always
        if (seeder is not IEnvironmentAwareSeeder environmentAware)
        {
            return true;
        }

        // Check if current environment is in allowed environments
        var isAllowed = environmentAware.AllowedEnvironments.Contains(_environment, StringComparer.OrdinalIgnoreCase);

        if (!isAllowed)
        {
            _logger.LogInformation("⊘ Skipped {SeederName} (not allowed in {Environment} environment)",
                seeder.Name, _environment);
        }

        return isAllowed;
    }
}
