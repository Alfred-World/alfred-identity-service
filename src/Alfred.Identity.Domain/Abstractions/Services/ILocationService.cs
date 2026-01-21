namespace Alfred.Identity.Domain.Abstractions.Services;

/// <summary>
/// Service for detecting location from IP address
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Get location information from IP address
    /// </summary>
    /// <param name="ipAddress">IP address to lookup</param>
    /// <returns>Location string (e.g., "Hanoi, Vietnam" or "New York, US")</returns>
    Task<string?> GetLocationFromIpAsync(string ipAddress);
}
