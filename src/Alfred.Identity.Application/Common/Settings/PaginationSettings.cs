namespace Alfred.Identity.Application.Common.Settings;

/// <summary>
/// Configuration settings for pagination across the application.
/// Can be configured in appsettings.json under "Pagination" section.
/// </summary>
public class PaginationSettings
{
    public const string SectionName = "Pagination";

    /// <summary>
    ///     Default page size when not specified
    /// </summary>
    public static int DefaultPageSize { get; set; } = 20;

    /// <summary>
    ///     Maximum allowed page size to prevent performance issues
    /// </summary>
    public static int MaxPageSize { get; set; } = 100;

    /// <summary>
    ///     Minimum page size
    /// </summary>
    public static int MinPageSize { get; } = 1;

    /// <summary>
    ///     Clamp page size to valid range
    /// </summary>
    public static int ClampPageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return DefaultPageSize;
        }

        return Math.Clamp(pageSize, MinPageSize, MaxPageSize);
    }

    /// <summary>
    ///     Ensure valid page number (1-based)
    /// </summary>
    public static int EnsureValidPage(int page)
    {
        return Math.Max(1, page);
    }
}
