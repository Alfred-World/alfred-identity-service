using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Alfred.Identity.WebApi.Services;

/// <summary>
/// Service for managing one-time auth tokens used in Token Exchange Pattern
/// </summary>
public interface IAuthTokenService
{
    /// <summary>
    /// Generate a one-time auth token and store user data
    /// </summary>
    string GenerateToken(AuthTokenData data);
    
    /// <summary>
    /// Validate and consume a one-time token, returning the associated data
    /// </summary>
    AuthTokenData? ValidateAndConsumeToken(string token);
}

/// <summary>
/// In-memory implementation of auth token service.
/// In production, use Redis or distributed cache.
/// </summary>
public sealed class InMemoryAuthTokenService : IAuthTokenService
{
    private readonly ConcurrentDictionary<string, AuthTokenData> _tokenCache = new();
    
    public string GenerateToken(AuthTokenData data)
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var token = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
        
        _tokenCache.TryAdd(token, data);
        return token;
    }
    
    public AuthTokenData? ValidateAndConsumeToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;
            
        if (!_tokenCache.TryRemove(token, out var tokenData))
            return null;
            
        // Check if token is expired
        if (DateTime.UtcNow > tokenData.ExpiresAt)
            return null;
            
        return tokenData;
    }
}

/// <summary>
/// Data stored with one-time auth token for Token Exchange Pattern
/// </summary>
public sealed record AuthTokenData
{
    public long UserId { get; init; }
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string? UserName { get; init; }
    public bool RememberMe { get; init; }
    public DateTime ExpiresAt { get; init; }
}
