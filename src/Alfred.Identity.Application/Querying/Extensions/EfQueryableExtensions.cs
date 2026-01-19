using Alfred.Identity.Application.Querying.Binding;
using Alfred.Identity.Application.Querying.Parsing;
using Alfred.Identity.Application.Querying.Validation;

namespace Alfred.Identity.Application.Querying.Extensions;

/// <summary>
/// Extension methods để apply filter/sort/paging cho IQueryable (EF Core)
/// </summary>
public static class EfQueryableExtensions
{
    /// <summary>
    /// Apply toàn bộ query (filter, sort, paging) lên IQueryable
    /// </summary>
    public static IQueryable<T> ApplyQuery<T>(
        this IQueryable<T> query,
        QueryRequest request,
        FieldMap<T> fieldMap,
        IFilterParser parser,
        int maxPageSize = 100)
    {
        // 1) Apply filter
        if (!string.IsNullOrWhiteSpace(request.Filter))
        {
            var ast = parser.Parse(request.Filter);
            FilterValidator.Validate(ast, fieldMap);
            var predicate = EfFilterBinder<T>.Bind(ast, fieldMap);
            query = query.Where(predicate);
        }

        // 2) Apply sorting
        query = SortBinder.ApplySort(query, request.Sort, fieldMap);

        // 3) Apply paging
        var pageSize = Math.Min(request.PageSize, maxPageSize);
        var skip = (request.Page - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        return query;
    }

    /// <summary>
    /// Apply filter only
    /// </summary>
    public static IQueryable<T> ApplyFilter<T>(
        this IQueryable<T> query,
        string? filter,
        FieldMap<T> fieldMap,
        IFilterParser parser)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return query;
        }

        var ast = parser.Parse(filter);
        FilterValidator.Validate(ast, fieldMap);
        var predicate = EfFilterBinder<T>.Bind(ast, fieldMap);
        return query.Where(predicate);
    }

    /// <summary>
    /// Apply sorting only
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sort,
        FieldMap<T> fieldMap)
    {
        return SortBinder.ApplySort(query, sort, fieldMap);
    }

    /// <summary>
    /// Apply paging only
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        int maxPageSize = 100)
    {
        var effectivePageSize = Math.Min(pageSize, maxPageSize);
        var skip = (page - 1) * effectivePageSize;
        return query.Skip(skip).Take(effectivePageSize);
    }
}
