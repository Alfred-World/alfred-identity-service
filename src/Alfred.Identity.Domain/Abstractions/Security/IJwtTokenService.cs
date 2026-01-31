namespace Alfred.Identity.Domain.Abstractions.Security;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Gets the lifetime of an access token in seconds
    /// </summary>
    int AccessTokenLifetimeSeconds { get; }

    /// <summary>
    /// Gets the lifetime of a refresh token in seconds
    /// </summary>
    int RefreshTokenLifetimeSeconds { get; }

    /// <summary>
    /// Generate an access token for a user (async for RSA key retrieval)
    /// </summary>
    Task<string> GenerateAccessTokenAsync(Guid userId, string email, string? fullName, Guid? applicationId = null);

    /// <summary>
    /// Generate a refresh token
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Get the JWT ID (jti) from a token
    /// </summary>
    string? GetJwtIdFromToken(string token);

    /// <summary>
    /// Validate a token and return claims if valid (async for key retrieval)
    /// </summary>
    Task<TokenValidationResult> ValidateTokenAsync(string token);

    /// <summary>
    /// Hash a refresh token for secure storage
    /// </summary>
    string HashRefreshToken(string token);

    /// <summary>
    /// Generate an ID token for OIDC (contains user identity claims)
    /// </summary>
    Task<string> GenerateIdTokenAsync(Guid userId, string email, string? fullName, string clientId,
        string? nonce = null);
}

/// <summary>
/// Result of token validation
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? JwtId { get; set; }
    public string? Error { get; set; }
}
