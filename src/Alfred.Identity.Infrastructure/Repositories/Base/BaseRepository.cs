using System.Linq.Expressions;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Common.Interfaces;
using Alfred.Identity.Domain.Querying;
using Alfred.Identity.Infrastructure.Querying;

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

    public virtual IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, object>>[]? includes)
    {
        var query = GetQueryable();
        if (includes != null)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
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
    /// Build a search query from JSON DSL FilterNode with structured sort fields.
    /// All filter binding and sort expression building happen here in Infrastructure.
    /// </summary>
    public virtual async Task<(IQueryable<TEntity> Query, long Total)> BuildSearchQueryAsync(
        FilterNode? filter,
        IReadOnlyList<SortField>? order,
        int page,
        int pageSize,
        IFieldResolver<TEntity> fieldResolver,
        Expression<Func<TEntity, object>>[]? includes = null,
        Expression<Func<TEntity, bool>>? preFilter = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbSet;

        // Apply soft delete filter
        if (typeof(IHasDeletionTime).IsAssignableFrom(typeof(TEntity)))
        {
            query = query.Where(e => !((IHasDeletionTime)e).IsDeleted);
        }

        // Apply pre-filter (business-level filter applied before DSL)
        if (preFilter != null)
        {
            query = query.Where(preFilter);
        }

        // Apply JSON DSL filter (at DB level)
        if (filter != null)
        {
            var filterExpression = FilterExpressionBinder<TEntity>.Bind(filter, fieldResolver);
            query = query.Where(filterExpression);
        }

        // Apply includes
        if (includes != null)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }

        // Get total count (after filtering, before pagination)
        var total = await query.LongCountAsync(cancellationToken);

        // Apply sorting
        query = SortExpressionBinder<TEntity>.Apply(query, order, fieldResolver);

        // Apply pagination
        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        return (query, total);
    }

    #endregion
}
