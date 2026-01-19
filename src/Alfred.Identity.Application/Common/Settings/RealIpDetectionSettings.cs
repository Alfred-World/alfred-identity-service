namespace FAM.Application.Settings;

/// <summary>
/// Settings for Real IP detection behind proxies (Nginx, Cloudflare, etc.)
/// Configured in appsettings.json under "RealIpDetection" section.
/// </summary>
public class RealIpDetectionSettings
{
    public const string SectionName = "RealIpDetection";

    /// <summary>
    /// List of trusted proxy IPs. Default: ["127.0.0.1", "::1"]
    /// </summary>
    public List<string> TrustedProxies { get; set; } = new() { "127.0.0.1", "::1" };

    /// <summary>
    /// Priority order of headers to check for real client IP.
    /// </summary>
    public List<string> HeaderPriority { get; set; } = new()
    {
        "CF-Connecting-IP",
        "True-Client-IP",
        "X-Real-IP",
        "X-Forwarded-For",
        "X-Client-IP"
    };

    /// <summary>
    /// Enable logging of IP detection for debugging.
    /// </summary>
    public bool LogIpDetection { get; set; } = false;
}
