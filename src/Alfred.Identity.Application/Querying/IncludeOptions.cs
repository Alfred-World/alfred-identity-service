namespace Alfred.Identity.Application.Querying;

/// <summary>
/// Options to configure include behavior
/// </summary>
public sealed record IncludeOptions
{
    /// <summary>
    /// Whitelist of relationships that are allowed to be included (case-insensitive)
    /// </summary>
    public HashSet<string> AllowedIncludes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maximum depth for nested includes (e.g., "departments.manager" has depth = 2)
    /// </summary>
    public int MaxDepth { get; init; } = 2;

    /// <summary>
    /// Maximum number of includes allowed in a single request
    /// </summary>
    public int MaxIncludesCount { get; init; } = 5;
}
