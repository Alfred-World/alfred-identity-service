using System.Net;
using System.Text.Json;

using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Infrastructure.Services.Dtos;

using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Services;

/// <summary>
/// Location service implementation using ip-api.com
/// Free tier: 45 requests per minute
/// </summary>
public class IpApiLocationService : ILocationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IpApiLocationService> _logger;
    private const string ApiBaseUrl = "https://get.geojs.io/v1/ip/geo/";

    public IpApiLocationService(HttpClient httpClient, ILogger<IpApiLocationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(5); // Short timeout to avoid blocking auth flow
    }

    public async Task<string?> GetLocationFromIpAsync(string ipAddress)
    {
        try
        {
            // Skip for local/private IPs or null
            if (string.IsNullOrEmpty(ipAddress)) return "Unknown";
            
            // Basic validation
            if (!IPAddress.TryParse(ipAddress, out _)) return "Invalid IP";

            // Localhost check logic
            if (ipAddress == "::1" || ipAddress == "127.0.0.1" || ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("10."))
            {
                return "Local Network";
            }

            string url = $"{ApiBaseUrl}{ipAddress}.json";
            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("IP API returned status code: {StatusCode} for IP: {IpAddress}",
                    response.StatusCode, ipAddress);
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            IpApiResponse? apiResponse = JsonSerializer.Deserialize<IpApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse == null)
            {
                return null;
            }

            return FormatLocation(apiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get location for IP: {IpAddress}", ipAddress);
            return null;
        }
    }

    private static string FormatLocation(IpApiResponse response)
    {
        if (!string.IsNullOrEmpty(response.City) && !string.IsNullOrEmpty(response.Country))
        {
            return $"{response.City}, {response.Country}";
        }

        if (!string.IsNullOrEmpty(response.Country))
        {
            return response.Country;
        }

        return "Unknown";
    }
}
