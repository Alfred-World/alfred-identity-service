namespace Alfred.Identity.WebApi.Configuration;

/// <summary>
/// Centralized configuration for SENSITIVE settings loaded from environment variables.
/// Non-sensitive settings are loaded from appsettings.json via IConfiguration.
/// </summary>
public class AppConfiguration
{
    // Database (from .env - sensitive)
    public string DatabaseProvider { get; }
    public string SqlServerConnectionString { get; }

    // Database Components
    public string DbHost { get; }
    public int DbPort { get; }
    public string DbName { get; }
    public string DbUser { get; }
    public string DbPassword { get; }

    // Application Settings
    public string AppHostname { get; }
    public int AppPort { get; }

    // CORS Settings
    public string[] CorsAllowedOrigins { get; }

    // Environment
    public string Environment { get; }
    public bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
    public string AppUrl => $"{(IsDevelopment ? "http" : "https")}://{AppHostname}:{AppPort}";

    public AppConfiguration()
    {
        // Environment
        Environment = GetOptional("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Database Provider
        DatabaseProvider = GetOptional("DB_PROVIDER") ?? "SqlServer";
        ValidateDatabaseProvider(DatabaseProvider);

        // Database Connection Components (Required)
        DbHost = GetRequired("DB_HOST");
        DbPort = GetInt("DB_PORT", 1433);
        ValidatePort(DbPort);

        DbName = GetRequired("DB_NAME");
        DbUser = GetRequired("DB_USER");
        DbPassword = GetRequired("DB_PASSWORD");

        // Build connection string based on provider
        SqlServerConnectionString = BuildSqlServerConnectionString();

        // Application Settings
        AppHostname = GetOptional("APP_HOSTNAME") ?? "*";
        AppPort = GetInt("APP_PORT", 8000);
        ValidatePort(AppPort, "APP_PORT");

        // CORS Settings
        var corsOrigins = GetOptional("CORS_ALLOWED_ORIGINS");
        CorsAllowedOrigins = string.IsNullOrWhiteSpace(corsOrigins)
            ? Array.Empty<string>()
            : corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private string BuildSqlServerConnectionString()
    {
        return
            $"Server={DbHost},{DbPort};Database={DbName};User Id={DbUser};Password={DbPassword};TrustServerCertificate=True;";
    }

    private static string GetRequired(string key)
    {
        var value = System.Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required environment variable '{key}' is not set. Check your .env file.");
        }

        return value;
    }

    private static string GetOptional(string key)
    {
        return System.Environment.GetEnvironmentVariable(key) ?? string.Empty;
    }

    private static int GetInt(string key, int defaultValue)
    {
        var value = GetOptional(key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : int.Parse(value);
    }

    private static bool GetBool(string key, bool defaultValue)
    {
        var value = GetOptional(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateDatabaseProvider(string provider)
    {
        var validProviders = new[] { "SqlServer", "PostgreSQL", "MySQL" };
        if (!validProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Invalid DB_PROVIDER '{provider}'. Valid values: {string.Join(", ", validProviders)}");
        }
    }

    private static void ValidatePort(int port, string portName = "DB_PORT")
    {
        if (port <= 0 || port > 65535)
        {
            throw new InvalidOperationException(
                $"Invalid {portName} '{port}'. Port must be between 1 and 65535.");
        }
    }
}
