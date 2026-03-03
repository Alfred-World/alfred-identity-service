namespace Alfred.Identity.Infrastructure.Common.Options;

/// <summary>
/// Consolidated JWT configuration. All values are read from environment variables
/// via AppConfiguration and registered as a singleton during startup.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// Token issuer — must match AUTH_VALID_ISSUER on the gateway.
    /// Env: Jwt__Issuer (Required)
    /// </summary>
    public required string Issuer { get; init; }

    /// <summary>
    /// Token audience.
    /// Env: Jwt__Audience (Required)
    /// </summary>
    public required string Audience { get; init; }

    /// <summary>
    /// Access token lifetime in seconds (default: 900 = 15 min).
    /// Env: Jwt__AccessTokenLifetimeSeconds (Optional)
    /// </summary>
    public int AccessTokenLifetimeSeconds { get; init; } = 900;

    /// <summary>
    /// Refresh token lifetime in seconds (default: 604800 = 7 days).
    /// Env: Jwt__RefreshTokenLifetimeSeconds (Optional)
    /// </summary>
    public int RefreshTokenLifetimeSeconds { get; init; } = 604800;
}
