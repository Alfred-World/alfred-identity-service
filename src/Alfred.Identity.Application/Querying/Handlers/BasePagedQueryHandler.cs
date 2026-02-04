using System.Linq.Expressions;

using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Filtering.Binding;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Application.Querying.Projection;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Common.Base;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Application.Querying.Handlers;

/// <summary>
/// Base handler for paginated queries with filtering, sorting, and projection.
/// Reduces code duplication across entity query handlers.
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TId">Entity ID type</typeparam>
/// <typeparam name="TDto">DTO type</typeparam>
/// <typeparam name="TQuery">Query request type</typeparam>
public abstract class BasePagedQueryHandler<TEntity, TId, TDto, TQuery>
    : IRequestHandler<TQuery, PageResult<TDto>>
    where TEntity : BaseEntity<TId>
    where TId : IEquatable<TId>
    where TDto : class, new()
    where TQuery : IRequest<PageResult<TDto>>
{
    protected readonly IFilterParser FilterParser;

    protected BasePagedQueryHandler(IFilterParser filterParser)
    {
        FilterParser = filterParser;
    }

    /// <summary>
    /// Get the repository for this entity
    /// </summary>
    protected abstract IRepository<TEntity, TId> Repository { get; }

    /// <summary>
    /// Get the field map for this entity
    /// </summary>
    protected abstract FieldMap<TEntity> FieldMap { get; }

    /// <summary>
    /// Get the view registry for this entity (optional - return null if no views defined)
    /// </summary>
    protected abstract ViewRegistry<TEntity, TDto>? ViewRegistry { get; }

    /// <summary>
    /// Extract QueryRequest from the query
    /// </summary>
    protected abstract QueryRequest GetQueryRequest(TQuery query);

    public virtual async Task<PageResult<TDto>> Handle(TQuery request, CancellationToken cancellationToken)
    {
        var queryRequest = GetQueryRequest(request);

        var page = queryRequest.GetEffectivePage();
        var pageSize = queryRequest.GetEffectivePageSize();

        // Get view from registry if available
        var view = ViewRegistry?.GetView(queryRequest.View);

        // Parse filter (at DB level)
        var filterExpression = ParseFilter(queryRequest.Filter);

        // Create field selector for sorting
        var fieldSelector = CreateFieldSelector();

        // Build paged query (filter, sort, paginate at DB level)
        var (query, total) = await Repository.BuildPagedQueryAsync(
            filterExpression,
            queryRequest.Sort,
            page,
            pageSize,
            view?.Includes,
            fieldSelector,
            cancellationToken
        );

        // Apply projection at DB level with AsNoTracking
        var items = await ExecuteQueryAsync(query.AsNoTracking(), view, cancellationToken);

        return new PageResult<TDto>(items, page, pageSize, total);
    }

    /// <summary>
    /// Execute the query and return DTOs.
    /// Override this for custom mapping logic.
    /// </summary>
    protected virtual async Task<List<TDto>> ExecuteQueryAsync(
        IQueryable<TEntity> query,
        ViewDefinition<TEntity, TDto>? view,
        CancellationToken cancellationToken)
    {
        if (view != null)
        {
            // Apply projection at DB level using view definition
            var projectedQuery = ProjectionBinder.ApplyProjection(query, view, FieldMap);
            return await projectedQuery.ToListAsync(cancellationToken);
        }

        // Fallback: load entities and map manually (less optimal)
        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Map entity to DTO (used when no view/projection is available)
    /// </summary>
    protected virtual TDto MapToDto(TEntity entity)
    {
        throw new InvalidOperationException(
            $"No view defined and MapToDto not overridden for {typeof(TEntity).Name}");
    }

    /// <summary>
    /// Parse filter string to expression
    /// </summary>
    protected Expression<Func<TEntity, bool>>? ParseFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return null;
        }

        try
        {
            var ast = FilterParser.Parse(filter);
            return EfFilterBinder<TEntity>.Bind(ast, FieldMap);
        }
        catch (InvalidOperationException ex)
        {
            throw FilterExceptionHelper.CreateFilterException(ex, filter, FieldMap);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid filter syntax: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Create field selector for sorting
    /// </summary>
    protected Func<string, (Expression<Func<TEntity, object>>? Expression, bool CanSort)> CreateFieldSelector()
    {
        return fieldName =>
        {
            if (FieldMap.TryGet(fieldName, out var expression, out _))
            {
                var canSort = FieldMap.CanSort(fieldName);
                var objectExpression = ExpressionConverterHelper.ConvertToObjectExpression<TEntity>(expression);
                return (objectExpression, canSort);
            }

            return (null, false);
        };
    }
}
