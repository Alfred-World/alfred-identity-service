using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Common.Interfaces;
using Alfred.Identity.Infrastructure.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories.Base;

/// <summary>
/// Generic repository implementation for EF Core
/// Consolidates BasePagedRepository, BaseRepository, and generic CRUD operations
/// </summary>
public abstract class BaseRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>
    where TId : IEquatable<TId>
{
    protected readonly IDbContext Context;
    protected readonly DbSet<TEntity> _dbSet;

    protected BaseRepository(IDbContext context)
    {
        Context = context;
        _dbSet = context.Set<TEntity>();
    }

    // Expose DbSet to derived classes for custom queries
    protected DbSet<TEntity> DbSet => _dbSet;

    #region IReadRepository Implementation

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (typeof(IHasDeletionTime).IsAssignableFrom(typeof(TEntity)))
        {
            return await _dbSet.AsNoTracking().Where(e => !((IHasDeletionTime)e).IsDeleted)
                .ToListAsync(cancellationToken);
        }

        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (typeof(IHasDeletionTime).IsAssignableFrom(typeof(TEntity)))
        {
            query = query.Where(e => !((IHasDeletionTime)e).IsDeleted);
        }

        return await query.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual IQueryable<TEntity> GetQueryable()
    {
        var query = _dbSet.AsQueryable();
        if (typeof(IHasDeletionTime).IsAssignableFrom(typeof(TEntity)))
        {
            query = query.Where(e => !((IHasDeletionTime)e).IsDeleted);
        }

        return query;
    }

    #endregion

    #region IRepository Implementation

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync([id], cancellationToken);

        if (entity != null && entity is IHasDeletionTime softDeleteEntity && softDeleteEntity.IsDeleted)
        {
            return null;
        }

        return entity;
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Delete(TEntity entity)
    {
        if (entity is IHasDeletionTime softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = true;
            softDeleteEntity.DeletedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
        }
        else
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        if (typeof(IHasDeletionTime).IsAssignableFrom(typeof(TEntity)))
        {
            query = query.Where(e => !((IHasDeletionTime)e).IsDeleted);
        }

        return await query.AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Build a query with filtering, sorting, pagination at database level.
    /// Returns the query + total count for Handler to apply projection.
    /// </summary>
    public virtual async Task<(IQueryable<TEntity> Query, long Total)> BuildPagedQueryAsync(
        Expression<Func<TEntity, bool>>? filter,
        string? sort,
        int page,
        int pageSize,
        Expression<Func<TEntity, object>>[]? includes,
        Func<string, (Expression<Func<TEntity, object>>? Expression, bool CanSort)>? fieldSelector,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        // Apply soft delete filter
        if (typeof(IHasDeletionTime).IsAssignableFrom(typeof(TEntity)))
        {
            query = query.Where(e => !((IHasDeletionTime)e).IsDeleted);
        }

        // Apply filter (at DB level)
        if (filter != null)
        {
            query = query.Where(filter);
        }

        // Apply includes (for nested projections)
        if (includes != null)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }

        // Get total count (before pagination, after filtering)
        var total = await query.CountAsync(cancellationToken);

        // Apply sorting (at DB level)
        if (string.IsNullOrWhiteSpace(sort))
        {
            query = query.OrderBy("Id");
        }
        else
        {
            if (fieldSelector != null)
            {
                query = ApplySortingWithFieldSelector(query, sort, fieldSelector);
            }
            else
            {
                try
                {
                    query = query.OrderBy(sort);
                }
                catch
                {
                    throw new ArgumentException($"Invalid sort expression: {sort}");
                }
            }
        }

        // Apply pagination (at DB level)
        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        return (query, total);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Apply dynamic sorting using field selector
    /// </summary>
    protected IQueryable<TEntity> ApplySortingWithFieldSelector(
        IQueryable<TEntity> query,
        string sort,
        Func<string, (Expression<Func<TEntity, object>>? Expression, bool CanSort)> fieldSelector)
    {
        var sortParts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<TEntity>? orderedQuery = null;

        foreach (var sortPart in sortParts)
        {
            var trimmed = sortPart.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            var descending = trimmed.StartsWith('-'); // Or ends with " desc"? Check convention. 
            // Standard convention often space separated "Name desc", but let's support user's logic if "Name desc" comes in.
            // Actually user's snippet logic: `var descending = trimmed.StartsWith('-');` implies "-Name". 
            // BUT Alfred API standard (from GetApplicationsQuery) is likely "Name desc" or "Name asc".
            // Let's SUPPORT BOTH conventions to be safe, or check GetApplicationsQueryHandler.

            // Standardizing on Dynamic Linq style parsing for safety if selector fails? 
            // Let's stick closer to the user's snippet logic but adapt for "Name desc" common case too.

            var isDescending = false;
            var fieldName = trimmed;

            if (trimmed.StartsWith('-'))
            {
                isDescending = true;
                fieldName = trimmed[1..];
            }
            else if (trimmed.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
            {
                isDescending = true;
                fieldName = trimmed[0..^5];
            }
            else if (trimmed.EndsWith(" asc", StringComparison.OrdinalIgnoreCase))
            {
                fieldName = trimmed[0..^4];
            }

            var (expression, canSort) = fieldSelector(fieldName);

            if (expression == null || !canSort)
            {
                continue;
            }

            orderedQuery = orderedQuery == null
                ? isDescending ? query.OrderByDescending(expression) : query.OrderBy(expression)
                : isDescending
                    ? orderedQuery.ThenByDescending(expression)
                    : orderedQuery.ThenBy(expression);
        }

        return orderedQuery ?? query.OrderBy("Id");
    }

    #endregion
}

/// <summary>
/// Generic repository implementation for entities with Guid Id
/// </summary>
public abstract class BaseRepository<TEntity> : BaseRepository<TEntity, Guid>, IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected BaseRepository(IDbContext context) : base(context)
    {
    }
}
