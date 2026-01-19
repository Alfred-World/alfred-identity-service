namespace Alfred.Identity.Application.Common.Helpers;

/// <summary>
/// Helper class for parsing include parameters
/// </summary>
public static class IncludeParser
{
    /// <summary>
    /// Parse comma-separated include string to HashSet with lowercase values
    /// </summary>
    /// <param name="include">Comma-separated include string (e.g., "devices,nodeRoles")</param>
    /// <returns>HashSet of lowercase include values, empty set if null/whitespace</returns>
    public static HashSet<string> Parse(string? include)
    {
        if (string.IsNullOrWhiteSpace(include))
        {
            return new HashSet<string>();
        }

        return include.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(i => i.Trim().ToLowerInvariant())
            .ToHashSet();
    }
}
