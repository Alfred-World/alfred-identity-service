using System.Linq.Expressions;

using Alfred.Identity.Domain.Common.Base;

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

    /// <summary>
    /// Build a query with filtering, sorting, pagination at database level.
    /// Returns query + total count - handlers can materialize or apply projection.
    /// </summary>
    Task<(IQueryable<T> Query, long Total)> BuildPagedQueryAsync(
        Expression<Func<T, bool>>? filter,
        string? sort,
        int page,
        int pageSize,
        Expression<Func<T, object>>[]? includes,
        Func<string, (Expression<Func<T, object>>? Expression, bool CanSort)>? fieldSelector,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "BuildPagedQueryAsync not implemented for this repository. Override in derived class if needed.");
    }

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for entities with Guid Id
/// </summary>
public interface IRepository<T> : IRepository<T, Guid> where T : BaseEntity
{
}
