namespace Alfred.Identity.Domain.Abstractions;

/// <summary>
/// Service for managing one-time auth tokens used in Token Exchange Pattern
/// </summary>
public interface IAuthTokenService
{
    /// <summary>
    /// Generate a one-time auth token and store user data
    /// </summary>
    Task<string> GenerateTokenAsync(AuthTokenData data);

    /// <summary>
    /// Validate and consume a one-time token, returning the associated data
    /// </summary>
    Task<AuthTokenData?> ValidateAndConsumeTokenAsync(string token);
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
