using System.Linq.Expressions;

using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Filtering.Binding;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Application.Querying.Projection;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Common.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Application.Common;

/// <summary>
/// Base service providing common pagination and filtering helpers for all application services.
/// Mirrors the pattern from alfred-core's BaseApplicationService.
/// </summary>
public abstract class BaseEntityService
{
    private readonly IFilterParser _filterParser;

    protected BaseEntityService(IFilterParser filterParser)
    {
        _filterParser = filterParser;
    }

    /// <summary>
    /// Execute a paginated query with DSL filtering and dynamic sorting.
    /// </summary>
    protected async Task<PageResult<TDto>> GetPagedAsync<TEntity, TDto>(
        IRepository<TEntity, Guid> repository,
        QueryRequest query,
        BaseFieldMap<TEntity> fieldMap,
        Func<TEntity, TDto> mapper,
        CancellationToken cancellationToken = default)
        where TEntity : BaseEntity<Guid>
    {
        return await GetPagedAsync(repository, query, fieldMap, null, null, mapper, cancellationToken);
    }

    /// <summary>
    /// Execute a paginated query with an optional pre-filter, DSL filtering, and sorting.
    /// </summary>
    protected async Task<PageResult<TDto>> GetPagedAsync<TEntity, TDto>(
        IRepository<TEntity, Guid> repository,
        QueryRequest query,
        BaseFieldMap<TEntity> fieldMap,
        Expression<Func<TEntity, bool>>? preFilter,
        Func<TEntity, TDto> mapper,
        CancellationToken cancellationToken = default)
        where TEntity : BaseEntity<Guid>
    {
        return await GetPagedAsync(repository, query, fieldMap, preFilter, null, mapper, cancellationToken);
    }

    /// <summary>
    /// Full overload with pre-filter + includes support.
    /// </summary>
    protected async Task<PageResult<TDto>> GetPagedAsync<TEntity, TDto>(
        IRepository<TEntity, Guid> repository,
        QueryRequest query,
        BaseFieldMap<TEntity> fieldMap,
        Expression<Func<TEntity, bool>>? preFilter,
        Expression<Func<TEntity, object>>[]? includes,
        Func<TEntity, TDto> mapper,
        CancellationToken cancellationToken = default)
        where TEntity : BaseEntity<Guid>
    {
        var page = query.GetEffectivePage();
        var pageSize = query.GetEffectivePageSize();
        var fields = fieldMap.Fields;

        Expression<Func<TEntity, bool>>? dslFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Filter))
        {
            try
            {
                var ast = _filterParser.Parse(query.Filter);
                dslFilter = EfFilterBinder<TEntity>.Bind(ast, fields);
            }
            catch (InvalidOperationException ex)
            {
                throw FilterExceptionHelper.CreateFilterException(ex, query.Filter, fields);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid filter syntax: {ex.Message}", ex);
            }
        }

        var combinedFilter = CombineFilters(preFilter, dslFilter);

        Func<string, (Expression<Func<TEntity, object>>? Expression, bool CanSort)> fieldSelector = fieldName =>
        {
            if (fields.TryGet(fieldName, out var expression, out _))
            {
                var canSort = fields.CanSort(fieldName);
                var objectExpression = ExpressionConverterHelper.ConvertToObjectExpression<TEntity>(expression);
                return (objectExpression, canSort);
            }

            return (null, false);
        };

        var (dbQuery, total) = await repository.BuildPagedQueryAsync(
            combinedFilter,
            query.Sort,
            page,
            pageSize,
            includes,
            fieldSelector,
            cancellationToken);

        var entities = await dbQuery.AsNoTracking().ToListAsync(cancellationToken);
        var items = entities.Select(mapper).ToList();

        return new PageResult<TDto>(items, page, pageSize, total);
    }

    #region View/Projection overloads

    /// <summary>
    /// Execute a paginated query with View/Projection support.
    /// Resolves query.View from the ViewRegistry and applies DB-level projection via ProjectionBinder.
    /// Falls back to in-memory mapper when no view is matched.
    /// </summary>
    protected async Task<PageResult<TDto>> GetPagedWithViewAsync<TEntity, TDto>(
        IRepository<TEntity, Guid> repository,
        QueryRequest query,
        BaseFieldMap<TEntity> fieldMap,
        ViewRegistry<TEntity, TDto>? viewRegistry,
        Func<TEntity, TDto> fallbackMapper,
        CancellationToken cancellationToken = default)
        where TEntity : BaseEntity<Guid>
        where TDto : class, new()
    {
        return await GetPagedWithViewAsync(repository, query, fieldMap, null, viewRegistry, fallbackMapper,
            cancellationToken);
    }

    /// <summary>
    /// Execute a paginated query with pre-filter and View/Projection support.
    /// </summary>
    protected async Task<PageResult<TDto>> GetPagedWithViewAsync<TEntity, TDto>(
        IRepository<TEntity, Guid> repository,
        QueryRequest query,
        BaseFieldMap<TEntity> fieldMap,
        Expression<Func<TEntity, bool>>? preFilter,
        ViewRegistry<TEntity, TDto>? viewRegistry,
        Func<TEntity, TDto> fallbackMapper,
        CancellationToken cancellationToken = default)
        where TEntity : BaseEntity<Guid>
        where TDto : class, new()
    {
        var page = query.GetEffectivePage();
        var pageSize = query.GetEffectivePageSize();
        var fields = fieldMap.Fields;

        // 1. Parse DSL filter
        Expression<Func<TEntity, bool>>? dslFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Filter))
        {
            try
            {
                var ast = _filterParser.Parse(query.Filter);
                dslFilter = EfFilterBinder<TEntity>.Bind(ast, fields);
            }
            catch (InvalidOperationException ex)
            {
                throw FilterExceptionHelper.CreateFilterException(ex, query.Filter, fields);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid filter syntax: {ex.Message}", ex);
            }
        }

        var combinedFilter = CombineFilters(preFilter, dslFilter);

        // 2. Resolve view (if any)
        ViewDefinition<TEntity, TDto>? view = null;
        if (viewRegistry != null)
        {
            try
            {
                view = viewRegistry.GetView(query.View);
            }
            catch (InvalidOperationException)
            {
                // No default view set and no view requested — fall back to full entity load
                view = null;
            }
        }

        // 3. Merge includes from view definition
        var includes = view?.Includes;

        // 4. Build field selector for sorting
        Func<string, (Expression<Func<TEntity, object>>? Expression, bool CanSort)> fieldSelector = fieldName =>
        {
            if (fields.TryGet(fieldName, out var expression, out _))
            {
                var canSort = fields.CanSort(fieldName);
                var objectExpression = ExpressionConverterHelper.ConvertToObjectExpression<TEntity>(expression);
                return (objectExpression, canSort);
            }

            return (null, false);
        };

        // 5. Build paged query (filter + sort + page)
        var (dbQuery, total) = await repository.BuildPagedQueryAsync(
            combinedFilter,
            query.Sort,
            page,
            pageSize,
            includes,
            fieldSelector,
            cancellationToken);

        // 6. Apply projection or fallback mapper
        if (view != null)
        {
            // DB-level projection — only SELECT the fields defined in the view
            var projected = ProjectionBinder.ApplyProjection(dbQuery.AsNoTracking(), view, fields);
            var items = await projected.ToListAsync(cancellationToken);
            return new PageResult<TDto>(items, page, pageSize, total);
        }
        else
        {
            // In-memory mapping — full entity load
            var entities = await dbQuery.AsNoTracking().ToListAsync(cancellationToken);
            var items = entities.Select(fallbackMapper).ToList();
            return new PageResult<TDto>(items, page, pageSize, total);
        }
    }

    #endregion

    private static Expression<Func<TEntity, bool>>? CombineFilters<TEntity>(
        Expression<Func<TEntity, bool>>? left,
        Expression<Func<TEntity, bool>>? right)
    {
        if (left == null)
        {
            return right;
        }

        if (right == null)
        {
            return left;
        }

        var param = left.Parameters[0];
        var rightBody = new ParameterReplacerVisitor(right.Parameters[0], param).Visit(right.Body);
        return Expression.Lambda<Func<TEntity, bool>>(Expression.AndAlso(left.Body, rightBody), param);
    }

    private sealed class ParameterReplacerVisitor(ParameterExpression oldParam, ParameterExpression newParam)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == oldParam ? newParam : base.VisitParameter(node);
        }
    }
}
