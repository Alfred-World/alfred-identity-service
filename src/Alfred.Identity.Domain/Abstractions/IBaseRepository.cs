using System.Linq.Expressions;

using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Domain.Abstractions;

/// <summary>
/// Base repository interface with common read operations
/// </summary>
public interface IReadRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get IQueryable for advanced filtering and querying
    /// </summary>
    IQueryable<T> GetQueryable()
    {
        throw new NotImplementedException(
            "GetQueryable not implemented for this repository. Override in derived class if needed.");
    }

    /// <summary>
    /// Get IQueryable with eager-loaded navigation properties applied (Include chains are
    /// handled in the Infrastructure layer so the Application layer stays EF-free).
    /// </summary>
    IQueryable<T> GetQueryable(Expression<Func<T, object>>[]? includes)
    {
        return GetQueryable();
    }
}

/// <summary>
/// Repository for entities with Id (any type)
/// </summary>
public interface IRepository<T, TId> : IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Build a search query from JSON DSL FilterNode, with sorting, pagination at database level.
    /// Filter binding and sort expression building are handled entirely in Infrastructure.
    /// </summary>
    Task<(IQueryable<T> Query, long Total)> BuildSearchQueryAsync(
        FilterNode? filter,
        IReadOnlyList<SortField>? order,
        int page,
        int pageSize,
        IFieldResolver<T> fieldResolver,
        Expression<Func<T, object>>[]? includes = null,
        Expression<Func<T, bool>>? preFilter = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "BuildSearchQueryAsync not implemented for this repository. Override in derived class if needed.");
    }
}
