using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Common.Seeding;

/// <summary>
/// Base class for data seeders with common functionality.
/// Seeders are ordered by Name (use timestamp prefix format: "20251129140000_SeederName").
/// </summary>
public abstract class BaseDataSeeder : IDataSeeder
{
    protected readonly ILogger Logger;

    protected BaseDataSeeder(ILogger logger)
    {
        Logger = logger;
    }

    public abstract Task SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Name of the seeder (used for ordering and tracking).
    /// Should be in format: "{timestamp}_{SeederName}" (e.g., "20251129140000_SigningKeySeeder")
    /// </summary>
    public abstract string Name { get; }

    protected void LogInfo(string message)
    {
        Logger.LogInformation("[{SeederName}] {Message}", Name, message);
    }

    protected void LogWarning(string message)
    {
        Logger.LogWarning("[{SeederName}] {Message}", Name, message);
    }

    protected void LogError(string message, Exception? ex = null)
    {
        if (ex != null)
        {
            Logger.LogError(ex, "[{SeederName}] {Message}", Name, message);
        }
        else
        {
            Logger.LogError("[{SeederName}] {Message}", Name, message);
        }
    }

    protected void LogSuccess()
    {
        Logger.LogInformation("[{SeederName}] ✓ Completed successfully", Name);
    }
}
