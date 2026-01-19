using System.ComponentModel;

using Alfred.Identity.Application.Querying;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace FAM.WebApi.Contracts.Common;

/// <summary>
/// Standard pagination query parameters for list endpoints
/// Supports filtering, sorting, pagination, field projection, and includes
/// Follows auth-service pattern for consistent API interface
/// </summary>
public sealed record PaginationQueryParameters
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [FromQuery(Name = "page")]
    [DefaultValue(1)]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [FromQuery(Name = "pageSize")]
    [DefaultValue(20)]
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Filter expression using DSL syntax
    /// Example: "isActive == true and name @contains('test')"
    /// </summary>
    [FromQuery(Name = "filter")]
    [SwaggerParameter(
        "Filter expression using DSL syntax. Examples: \"name @contains('abc')\", \"phone == '123' or phone == '321'\")")]
    public string? Filter { get; init; }

    /// <summary>
    /// Sort expression (comma-separated)
    /// Example: "name,-createdAt" (ascending by name, descending by createdAt)
    /// </summary>
    [FromQuery(Name = "sort")]
    [SwaggerParameter(
        "Sort expression. Use '-' prefix for descending. Examples: \"resource\", \"-createdAt\", \"resource,-action\"")]
    public string? Sort { get; init; }

    /// <summary>
    /// Related entities to include (comma-separated)
    /// Example: "createdBy,updatedBy"
    /// </summary>
    [FromQuery(Name = "include")]
    [SwaggerParameter("Comma-separated related entities. Example: \"createdBy,updatedBy\"")]
    public string? Include { get; init; }
}

/// <summary>
///     Extension methods for building QueryRequest from query parameters
/// </summary>
public static class QueryRequestExtensions
{
    /// <summary>
    ///     Convert PaginationQueryParameters to QueryRequest
    /// </summary>
    public static QueryRequest ToQueryRequest(this PaginationQueryParameters parameters)
    {
        return new QueryRequest
        {
            Filter = parameters.Filter ?? string.Empty,
            Sort = parameters.Sort ?? string.Empty,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            Include = parameters.Include ?? string.Empty
        };
    }
}
