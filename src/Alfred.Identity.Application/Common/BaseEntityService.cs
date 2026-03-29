using System.Linq.Expressions;

using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Filtering.Binding;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Application.Querying.Projection;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Common.Base;

namespace Alfred.Identity.Application.Common;

/// <summary>
/// Base service providing common pagination and filtering helpers for all application services.
/// Mirrors the pattern from alfred-core's BaseApplicationService.
/// </summary>
public abstract class BaseEntityService
{
    private readonly IFilterParser _filterParser;
    protected readonly IAsyncQueryExecutor _executor;

    protected BaseEntityService(IFilterParser filterParser, IAsyncQueryExecutor executor)
    {
        _filterParser = filterParser;
        _executor = executor;
    }

    /// <summary>
    /// Execute a paginated query with DSL filtering, dynamic sorting, and optional pre-filter / includes.
    /// </summary>
    protected async Task<PageResult<TDto>> GetPagedAsync<TEntity, TId, TDto>(
        IRepository<TEntity, TId> repository,
        QueryRequest query,
        BaseFieldMap<TEntity> fieldMap,
        Func<TEntity, TDto> mapper,
        CancellationToken cancellationToken = default,
        Expression<Func<TEntity, bool>>? preFilter = null,
        Expression<Func<TEntity, object>>[]? includes = null)
        where TEntity : BaseEntity<TId>
        where TId : IEquatable<TId>
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

        var entities = await _executor.ToListAsync(_executor.AsNoTracking(dbQuery), cancellationToken);
        var items = entities.Select(mapper).ToList();

        return new PageResult<TDto>(items, page, pageSize, total);
    }

    #region View/Projection overloads

    /// <summary>
    /// Execute a paginated query with View/Projection support and optional pre-filter.
    /// Resolves query.View from the ViewRegistry and applies DB-level projection via ProjectionBinder.
    /// Falls back to in-memory mapper when no view is matched.
    /// </summary>
    protected async Task<PageResult<TDto>> GetPagedWithViewAsync<TEntity, TId, TDto>(
        IRepository<TEntity, TId> repository,
        QueryRequest query,
        BaseFieldMap<TEntity> fieldMap,
        ViewRegistry<TEntity, TDto>? viewRegistry,
        Func<TEntity, TDto> fallbackMapper,
        CancellationToken cancellationToken = default,
        Expression<Func<TEntity, bool>>? preFilter = null)
        where TEntity : BaseEntity<TId>
        where TId : IEquatable<TId>
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
            var projected = ProjectionBinder.ApplyProjection(_executor.AsNoTracking(dbQuery), view, fields);
            var items = await _executor.ToListAsync(projected, cancellationToken);
            return new PageResult<TDto>(items, page, pageSize, total);
        }
        else
        {
            // In-memory mapping — full entity load
            var entities = await _executor.ToListAsync(_executor.AsNoTracking(dbQuery), cancellationToken);
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
