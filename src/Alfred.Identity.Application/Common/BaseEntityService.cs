using System.Linq.Expressions;

using Alfred.Identity.Application.Common.Settings;
using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Application.Common;

/// <summary>
/// Base service providing common pagination and filtering helpers for all application services.
/// </summary>
public abstract class BaseEntityService
{
    protected readonly IAsyncQueryExecutor _executor;

    protected BaseEntityService(IAsyncQueryExecutor executor)
    {
        _executor = executor;
    }

    #region Search (POST)

    /// <summary>
    /// Execute a search query using JSON DSL (POST body) with structured filter/sort.
    /// Filter binding and sort expression building are delegated to Infrastructure.
    /// </summary>
    protected async Task<PageResult<TDto>> SearchAsync<TEntity, TId, TDto>(
        IRepository<TEntity, TId> repository,
        SearchRequest request,
        BaseFieldMap<TEntity> fieldMap,
        Func<TEntity, TDto> mapper,
        CancellationToken cancellationToken = default,
        Expression<Func<TEntity, bool>>? preFilter = null,
        Expression<Func<TEntity, object>>[]? includes = null)
        where TEntity : BaseEntity<TId>
        where TId : IEquatable<TId>
    {
        var page = GetEffectivePage(request.Page);
        var pageSize = GetEffectivePageSize(request.PageSize);

        var (dbQuery, total) = await repository.BuildSearchQueryAsync(
            request.Filter,
            request.Order,
            page,
            pageSize,
            fieldMap.Fields,
            includes,
            preFilter,
            cancellationToken);

        var entities = await _executor.ToListAsync(_executor.AsNoTracking(dbQuery), cancellationToken);
        var items = entities.Select(mapper).ToList();

        return new PageResult<TDto>(items, page, pageSize, total);
    }

    /// <summary>
    /// Execute a search query with View/Projection support using JSON DSL.
    /// </summary>
    protected async Task<PageResult<TDto>> SearchWithViewAsync<TEntity, TId, TDto>(
        IRepository<TEntity, TId> repository,
        SearchRequest request,
        BaseFieldMap<TEntity> fieldMap,
        ViewRegistry<TEntity, TDto>? viewRegistry,
        Func<TEntity, TDto> fallbackMapper,
        CancellationToken cancellationToken = default,
        Expression<Func<TEntity, bool>>? preFilter = null)
        where TEntity : BaseEntity<TId>
        where TId : IEquatable<TId>
        where TDto : class, new()
    {
        var page = GetEffectivePage(request.Page);
        var pageSize = GetEffectivePageSize(request.PageSize);
        var fields = fieldMap.Fields;

        // Resolve view
        ViewDefinition<TEntity, TDto>? view = null;
        if (viewRegistry != null)
        {
            try
            {
                view = viewRegistry.GetView(request.View);
            }
            catch (InvalidOperationException)
            {
                view = null;
            }
        }

        var includes = view?.Includes;

        var (dbQuery, total) = await repository.BuildSearchQueryAsync(
            request.Filter,
            request.Order,
            page,
            pageSize,
            fields,
            includes,
            preFilter,
            cancellationToken);

        if (view != null)
        {
            var projected = ProjectionBinder.ApplyProjection(_executor.AsNoTracking(dbQuery), view, fields);
            var items = await _executor.ToListAsync(projected, cancellationToken);
            return new PageResult<TDto>(items, page, pageSize, total);
        }
        else
        {
            var entities = await _executor.ToListAsync(_executor.AsNoTracking(dbQuery), cancellationToken);
            var items = entities.Select(fallbackMapper).ToList();
            return new PageResult<TDto>(items, page, pageSize, total);
        }
    }

    /// <summary>
    /// Build SearchMetadataResponse for a given field map.
    /// </summary>
    protected static SearchMetadataResponse BuildSearchMetadata<TEntity>(BaseFieldMap<TEntity> fieldMap)
        where TEntity : class
    {
        IFieldResolver<TEntity> resolver = fieldMap.Fields;
        return new SearchMetadataResponse
        {
            TypeOperators = OperatorsByType.AllTypes,
            Fields = resolver.GetAllFieldMeta().ToList()
        };
    }

    private static int GetEffectivePage(int page)
    {
        return PaginationSettings.EnsureValidPage(page);
    }

    private static int GetEffectivePageSize(int pageSize)
    {
        return pageSize > 0 ? PaginationSettings.ClampPageSize(pageSize) : PaginationSettings.DefaultPageSize;
    }

    #endregion
}
