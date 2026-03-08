using System.Text.Json;

namespace Alfred.Identity.WebApi.Extensions;

/// <summary>
/// Shared URI parsing and matching utilities used by Auth and Connect controllers.
/// </summary>
public static class UriHelper
{
    /// <summary>
    /// Parse URI list from JSON array or space-delimited string.
    /// </summary>
    public static List<string> ParseUriList(string? uriString)
    {
        if (string.IsNullOrEmpty(uriString))
        {
            return [];
        }

        // Try JSON array first
        if (uriString.TrimStart().StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(uriString) ?? [];
            }
            catch
            {
                // Fall through to space-delimited
            }
        }

        // Space-delimited
        return uriString.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    /// <summary>
    /// Check if a URI matches an allowed pattern (exact or same host).
    /// </summary>
    public static bool UriMatches(string uri, string allowedPattern)
    {
        if (string.Equals(uri, allowedPattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Uri.TryCreate(uri, UriKind.Absolute, out var uriParsed) &&
            Uri.TryCreate(allowedPattern, UriKind.Absolute, out var allowedParsed))
        {
            return string.Equals(uriParsed.Host, allowedParsed.Host, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Truncate User-Agent string to a safe max length (256).
    /// </summary>
    public static string? TruncateDevice(string? userAgent)
    {
        return string.IsNullOrEmpty(userAgent) ? null
            : userAgent.Length <= 256 ? userAgent
            : userAgent[..256];
    }
}
