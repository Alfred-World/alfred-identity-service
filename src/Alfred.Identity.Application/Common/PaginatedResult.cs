namespace Alfred.Identity.Application.Common;

/// <summary>
/// Paginated result wrapper
/// </summary>
public class PaginatedResult<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int TotalDocs { get; set; }
    public int Limit { get; set; }
    public bool HasPrevPage { get; set; }
    public bool HasNextPage { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int Offset { get; set; }
    public int? PrevPage { get; set; }
    public int? NextPage { get; set; }
    public int PagingCounter { get; set; }
    public object? Meta { get; set; }
}
