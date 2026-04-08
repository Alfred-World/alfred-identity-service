using System.Text.Json.Serialization;

namespace Alfred.Identity.Domain.Querying;

/// <summary>
/// POST body for search endpoints.
/// Replaces the old GET-based PaginationQueryParameters with a structured JSON DSL.
/// </summary>
public sealed record SearchRequest
{
    /// <summary>Page number (1-based)</summary>
    public int Page { get; init; } = 1;

    /// <summary>Number of items per page</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// JSON filter using HotChocolate-inspired DSL.
    /// Example: { "and": [{ "email": { "contains": "admin" } }, { "isActive": { "eq": true } }] }
    /// </summary>
    public FilterNode? Filter { get; init; }

    /// <summary>
    /// Sort fields with direction.
    /// Example: [{ "field": "createdAt", "direction": "desc" }, { "field": "fullName", "direction": "asc" }]
    /// </summary>
    public IReadOnlyList<SortField>? Order { get; init; }

    /// <summary>
    /// View name to determine which fields to return (e.g., "list", "detail", "minimal").
    /// </summary>
    public string? View { get; init; }
}

/// <summary>
/// Typed search request for API endpoints.
/// Swagger/OpenAPI can describe <typeparamref name="TFilter"/> fields and operators,
/// enabling frontend codegen to produce autocomplete-friendly types.
/// Converts to the internal <see cref="SearchRequest"/> (with <see cref="FilterNode"/>) at the controller boundary.
/// </summary>
public sealed record SearchRequest<TFilter> where TFilter : class, IFilterInput
{
    /// <summary>Page number (1-based)</summary>
    public int Page { get; init; } = 1;

    /// <summary>Number of items per page</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>Typed filter input with operator properties.</summary>
    public TFilter? Filter { get; init; }

    /// <summary>Sort fields with direction.</summary>
    public IReadOnlyList<SortField>? Order { get; init; }

    /// <summary>View name to determine which fields to return.</summary>
    public string? View { get; init; }

    /// <summary>
    /// Convert to the internal non-generic <see cref="SearchRequest"/> used by the service/repository pipeline.
    /// </summary>
    public SearchRequest ToSearchRequest()
    {
        return new SearchRequest
        {
            Page = Page,
            PageSize = PageSize,
            Filter = Filter?.ToFilterNode(),
            Order = Order,
            View = View
        };
    }
}

/// <summary>
/// Single sort field with direction.
/// </summary>
public sealed record SortField
{
    public string Field { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SortDirection Direction { get; init; } = SortDirection.Asc;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortDirection
{
    Asc,
    Desc
}
