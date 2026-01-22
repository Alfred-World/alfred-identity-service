using System.Security.Cryptography;
using System.Text.Json;

using Alfred.Identity.Domain.Abstractions;

namespace Alfred.Identity.Infrastructure.Services;

/// <summary>
/// Auth token service implementation using ICacheProvider.
/// Uses the project's cache provider for consistency.
/// </summary>
public sealed class AuthTokenService : IAuthTokenService
{
    private readonly ICacheProvider _cacheProvider;
    private const string CacheKeyPrefix = "auth_token:";

    public AuthTokenService(ICacheProvider cacheProvider)
    {
        _cacheProvider = cacheProvider;
    }

    public async Task<string> GenerateTokenAsync(AuthTokenData data)
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var token = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Calculate TTL from ExpiresAt
        var ttl = data.ExpiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            ttl = TimeSpan.FromSeconds(60); // Default 60 seconds
        }

        // Serialize and store in cache
        var json = JsonSerializer.Serialize(data);
        await _cacheProvider.SetAsync(CacheKeyPrefix + token, json, ttl);

        return token;
    }

    public async Task<AuthTokenData?> ValidateAndConsumeTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        var cacheKey = CacheKeyPrefix + token;

        // Get and delete atomically (consume)
        var json = await _cacheProvider.GetAsync(cacheKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        // Delete the token (one-time use)
        await _cacheProvider.DeleteAsync(cacheKey);

        try
        {
            var tokenData = JsonSerializer.Deserialize<AuthTokenData>(json);
            if (tokenData == null)
            {
                return null;
            }

            // Check if token is expired
            if (DateTime.UtcNow > tokenData.ExpiresAt)
            {
                return null;
            }

            return tokenData;
        }
        catch
        {
            return null;
        }
    }
}
