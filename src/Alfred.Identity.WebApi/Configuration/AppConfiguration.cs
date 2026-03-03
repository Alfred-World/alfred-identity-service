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

    // URL Settings (Required — no silent defaults)
    /// <summary>Gateway public URL, e.g. https://gateway.lucasvu.io.vn</summary>
    public string GatewayUrl { get; }

    /// <summary>SSO/Identity Web URL, e.g. https://sso.lucasvu.io.vn</summary>
    public string SsoWebUrl { get; }

    /// <summary>Core/App Web URL, e.g. https://app.lucasvu.io.vn</summary>
    public string CoreWebUrl { get; }

    /// <summary>Identity Web URL (for password reset links), e.g. https://sso.lucasvu.io.vn</summary>
    public string IdentityWebUrl { get; }

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

        // URL Settings (Required — fail fast if missing)
        GatewayUrl = GetRequiredUrl("URLS_GATEWAY");
        SsoWebUrl = GetRequiredUrl("URLS_SSO_WEB");
        CoreWebUrl = GetRequiredUrl("URLS_CORE_WEB");
        IdentityWebUrl = GetRequiredUrl("URLS_IDENTITY_WEB");
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

    /// <summary>
    /// Reads a required URL env var while accepting the legacy "Urls__*" format from
    /// docker-compose (which maps to IConfiguration key "Urls:*") as well as the flat
    /// SCREAMING_SNAKE format. Order: URLS_GATEWAY > Urls__Gateway.
    /// </summary>
    private static string GetRequiredUrl(string key)
    {
        // Primary: URLS_GATEWAY
        var value = System.Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            // Fallback: docker-compose "Urls__Gateway" format → IConfiguration reads as Urls:Gateway
            // but we need the env var name with double underscores
            var legacyKey = ConvertToDoubleUnderscoreFormat(key); // URLS_GATEWAY → Urls__Gateway
            value = System.Environment.GetEnvironmentVariable(legacyKey);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"❌ Required URL environment variable '{key}' is not set. " +
                $"Set it in your .env file or docker-compose environment.");
        }

        // Strip trailing slash to prevent double-slash bugs  
        return value.TrimEnd('/');
    }

    /// <summary>
    /// URLS_GATEWAY → Urls__Gateway,  URLS_SSO_WEB → Urls__SsoWeb, etc.
    /// </summary>
    private static string ConvertToDoubleUnderscoreFormat(string key)
    {
        // URLS_GATEWAY     → ["URLS", "GATEWAY"]
        // URLS_SSO_WEB     → ["URLS", "SSO", "WEB"]
        // URLS_CORE_WEB    → ["URLS", "CORE", "WEB"]
        // URLS_IDENTITY_WEB → ["URLS", "IDENTITY", "WEB"]
        var parts = key.Split('_');
        if (parts.Length < 2) return key;

        var prefix = ToPascalCase(parts[0]); // "Urls"
        var rest = string.Join("", parts.Skip(1).Select(ToPascalCase)); // "Gateway", "SsoWeb" etc.

        return $"{prefix}__{rest}";
    }

    private static string ToPascalCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1).ToLowerInvariant();
    }
}
