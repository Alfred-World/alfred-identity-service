namespace Alfred.Identity.Application.Querying.Core;

/// <summary>
/// Paginated result wrapper
/// </summary>
public sealed record PageResult<T>
{
    /// <summary>
    /// Items in current page
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items (across all pages)
    /// </summary>
    public long Total { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;

    /// <summary>
    /// Has previous page?
    /// </summary>
    public bool HasPrevPage => Page > 1;

    /// <summary>
    /// Has next page?
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Previous page number (null if no previous page)
    /// </summary>
    public int? PrevPage => HasPrevPage ? Page - 1 : null;

    /// <summary>
    /// Next page number (null if no next page)
    /// </summary>
    public int? NextPage => HasNextPage ? Page + 1 : null;

    public PageResult()
    {
    }

    public PageResult(IReadOnlyList<T> items, int page, int pageSize, long total)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        Total = total;
    }
}
