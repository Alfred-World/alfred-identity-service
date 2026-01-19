namespace Alfred.Identity.Application.Common.Options;

/// <summary>
/// Paging configuration options
/// </summary>
public class PagingOptions
{
    /// <summary>
    /// Maximum allowed page size requested by clients. Requests with a larger limit
    /// will be clamped down to this value.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;
}
