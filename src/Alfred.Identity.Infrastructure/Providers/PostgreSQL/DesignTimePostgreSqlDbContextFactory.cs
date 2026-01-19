using Alfred.Identity.Infrastructure.Common.Options;

using Microsoft.EntityFrameworkCore.Design;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL;

/// <summary>
/// Design-time factory for PostgreSqlDbContext so EF tools can create migrations.
/// Loads configuration from .env file and environment variables.
/// </summary>
public class DesignTimePostgreSqlDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlDbContext>
{
    public PostgreSqlDbContext CreateDbContext(string[] args)
    {
        // Load .env file from project root
        LoadDotEnv();

        // Get connection components from environment variables
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "alfred_identity";
        var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";

        // Build PostgreSQL connection string
        var connectionString =
            $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};";

        // Validate port
        if (!int.TryParse(dbPort, out var port) || port < 1 || port > 65535)
        {
            throw new InvalidOperationException(
                $"DB_PORT must be a valid port number (1-65535). Current value: {dbPort}");
        }

        var enableDetailedErrors = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_DETAILED_ERRORS") ?? "false");
        var enableSensitiveDataLogging =
            bool.Parse(Environment.GetEnvironmentVariable("ENABLE_SENSITIVE_DATA_LOGGING") ?? "false");

        PostgreSqlOptions options = new()
        {
            ConnectionString = connectionString,
            EnableDetailedErrors = enableDetailedErrors,
            EnableSensitiveDataLogging = enableSensitiveDataLogging
        };

        Console.WriteLine($"Using PostgreSQL connection: {dbHost}:{dbPort}/{dbName} as {dbUser}");

        return new PostgreSqlDbContext(options);
    }

    private static void LoadDotEnv()
    {
        // Find the solution root directory (where .env is located)
        var currentDir = Directory.GetCurrentDirectory();
        var solutionRoot = FindSolutionRoot(currentDir);

        if (solutionRoot == null)
        {
            Console.WriteLine("Warning: Could not find solution root. Skipping .env file loading.");
            return;
        }

        var envFile = Path.Combine(solutionRoot, ".env");

        if (!File.Exists(envFile))
        {
            Console.WriteLine($"Warning: .env file not found at '{envFile}'. Using system environment variables only.");
            return;
        }

        Console.WriteLine($"Loading .env file from: {envFile}");

        foreach (var line in File.ReadAllLines(envFile))
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
            {
                continue;
            }

            var parts = trimmedLine.Split('=', 2, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                continue;
            }

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            // Remove quotes if present
            if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                value = value[1..^1];
            }

            // Only set if not already set (system env vars take precedence)
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        Console.WriteLine(".env file loaded successfully");
    }

    private static string? FindSolutionRoot(string startDirectory)
    {
        DirectoryInfo? current = new(startDirectory);

        while (current != null)
        {
            // Look for .sln file or .env file as indicators of solution root
            if (current.GetFiles("*.sln").Length > 0 ||
                current.GetFiles(".env").Length > 0)
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }
}
