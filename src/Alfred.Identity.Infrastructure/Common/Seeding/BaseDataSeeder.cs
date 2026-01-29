using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Common.Seeding;

/// <summary>
/// Base class for data seeders with common functionality.
/// Seeders are ordered by Name (use timestamp prefix format: "20251129140000_SeederName").
/// 
/// Logging strategy:
/// - Use LogDebug() for detailed progress (shown only on verbose/debug mode or on failure)
/// - Use LogInfo() for summary information after completion
/// - Use LogError() for failures (will show detailed context)
/// </summary>
public abstract class BaseDataSeeder : IDataSeeder
{
    protected readonly ILogger Logger;
    private readonly List<string> _debugMessages = new();

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

    /// <summary>
    /// Log debug message - stored for potential error reporting, not shown in normal output.
    /// </summary>
    protected void LogDebug(string message)
    {
        _debugMessages.Add(message);
        Logger.LogDebug("[{SeederName}] {Message}", Name, message);
    }

    /// <summary>
    /// Log informational message - shown in output (use sparingly for summary only).
    /// </summary>
    protected void LogInfo(string message)
    {
        Logger.LogInformation("[{SeederName}] {Message}", Name, message);
    }

    protected void LogWarning(string message)
    {
        Logger.LogWarning("[{SeederName}] {Message}", Name, message);
    }

    /// <summary>
    /// Log error with all debug context for troubleshooting.
    /// </summary>
    protected void LogError(string message, Exception? ex = null)
    {
        // On error, dump all debug messages for context
        if (_debugMessages.Count > 0)
        {
            Logger.LogError("[{SeederName}] Debug context:", Name);
            foreach (var debugMsg in _debugMessages)
            {
                Logger.LogError("  → {Message}", debugMsg);
            }
        }

        if (ex != null)
        {
            Logger.LogError(ex, "[{SeederName}] {Message}", Name, message);
        }
        else
        {
            Logger.LogError("[{SeederName}] {Message}", Name, message);
        }
    }

    /// <summary>
    /// Log success summary - single line showing completion.
    /// </summary>
    protected void LogSuccess(string? summary = null)
    {
        var msg = string.IsNullOrEmpty(summary)
            ? "✓ Completed"
            : $"✓ {summary}";
        Logger.LogInformation("[{SeederName}] {Message}", Name, msg);
        _debugMessages.Clear();
    }
}
