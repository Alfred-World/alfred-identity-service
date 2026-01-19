namespace Alfred.Identity.Application.Common.Settings;

/// <summary>
/// CORS (Cross-Origin Resource Sharing) configuration.
/// Configured in appsettings.json under "Cors" section.
/// </summary>
public class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Allowed origins (e.g., ["http://localhost:8001", "https://app.example.com"])
    /// </summary>
    public string[] AllowedOrigins { get; set; } = { "http://localhost:8001" };

    /// <summary>
    /// Allowed HTTP methods
    /// </summary>
    public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" };

    /// <summary>
    /// Allowed headers
    /// </summary>
    public string[] AllowedHeaders { get; set; } = { "Content-Type", "Authorization", "X-Requested-With" };

    /// <summary>
    /// Allow credentials (cookies, authorization headers)
    /// </summary>
    public bool AllowCredentials { get; set; } = true;
}
