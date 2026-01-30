using Alfred.Identity.Application.Common.Settings;

namespace Alfred.Identity.Application.Querying.Core;

/// <summary>
/// Common request for queries with filter, sort, paging, and includes
/// </summary>
public sealed record QueryRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size (number of items per page).
    /// Default and max values can be configured via PaginationSettings.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Filter DSL string (e.g.: "name @contains('printer') and price >= 100")
    /// </summary>
    public string? Filter { get; init; }

    /// <summary>
    /// Sort string (e.g.: "-createdAt,name" means sort by createdAt desc, then by name asc)
    /// </summary>
    public string? Sort { get; init; }

    /// <summary>
    /// View name to determine which fields to return (e.g.: "list", "detail", "minimal").
    /// Each entity defines available views via ViewRegistry.
    /// </summary>
    public string? View { get; init; }

    /// <summary>
    /// Get effective page size using global settings
    /// </summary>
    public int GetEffectivePageSize()
    {
        return PageSize > 0 ? PaginationSettings.ClampPageSize(PageSize) : PaginationSettings.DefaultPageSize;
    }

    /// <summary>
    /// Get effective page number
    /// </summary>
    public int GetEffectivePage()
    {
        return PaginationSettings.EnsureValidPage(Page);
    }
}
