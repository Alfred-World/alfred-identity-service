namespace Alfred.Identity.Application.Querying.Includes;

/// <summary>
/// Helper to parse and validate include parameters
/// </summary>
public static class IncludeHelper
{
    /// <summary>
    /// Parse include parameter string into a list of include paths
    /// </summary>
    /// <param name="includeParam">Include string (e.g.: "userNodeRoles,userDevices,assignments")</param>
    /// <param name="options">Options for validation</param>
    /// <returns>List of validated include paths</returns>
    public static List<string> ParseIncludes(string? includeParam, IncludeOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(includeParam))
        {
            return new List<string>();
        }

        var opts = options ?? new IncludeOptions();

        return includeParam
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Where(s => IsAllowed(s, opts))
            .Where(s => GetDepth(s) <= opts.MaxDepth)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(opts.MaxIncludesCount)
            .ToList();
    }

    /// <summary>
    /// Validate include parameter
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateIncludes(
        string? includeParam,
        IncludeOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(includeParam))
        {
            return (true, null);
        }

        var opts = options ?? new IncludeOptions();
        var includes = includeParam.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        // Check count
        if (includes.Count > opts.MaxIncludesCount)
        {
            return (false, $"Too many includes. Maximum allowed: {opts.MaxIncludesCount}");
        }

        // Check each include
        foreach (var include in includes)
        {
            // Check depth
            var depth = GetDepth(include);
            if (depth > opts.MaxDepth)
            {
                return (false, $"Include '{include}' exceeds max depth of {opts.MaxDepth}");
            }

            // Check if allowed
            if (opts.AllowedIncludes.Count > 0 && !IsAllowed(include, opts))
            {
                return (false, $"Include '{include}' is not allowed");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Calculate the depth of an include path (e.g.: "departments.manager" = 2)
    /// </summary>
    private static int GetDepth(string includePath)
    {
        return includePath.Split('.').Length;
    }

    /// <summary>
    /// Check if include path is allowed
    /// </summary>
    private static bool IsAllowed(string includePath, IncludeOptions options)
    {
        // If no whitelist is configured, allow all
        if (options.AllowedIncludes.Count == 0)
        {
            return true;
        }

        // Check exact match or prefix match (to support nested includes)
        return options.AllowedIncludes.Contains(includePath) ||
               options.AllowedIncludes.Any(allowed =>
                   includePath.StartsWith(allowed + ".", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Normalize include paths (lowercase, remove duplicates)
    /// </summary>
    public static List<string> NormalizeIncludes(IEnumerable<string> includes)
    {
        return includes
            .Select(i => i.Trim())
            .Where(i => !string.IsNullOrEmpty(i))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Check if a specific include path is present in the list of includes
    /// </summary>
    public static bool ShouldInclude(IEnumerable<string> includes, string path)
    {
        return includes.Any(i => i.Equals(path, StringComparison.OrdinalIgnoreCase));
    }
}
