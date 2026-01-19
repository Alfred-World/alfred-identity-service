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
    /// Get entities with filtering, sorting and pagination using custom field selector
    /// </summary>
    Task<(IEnumerable<T> Items, long Total)> GetPagedAsync(
        Expression<Func<T, bool>>? filter,
        string? sort,
        int page,
        int pageSize,
        IEnumerable<Expression<Func<T, object>>>? includes,
        Func<string, (Expression<Func<T, object>>? Expression, bool CanSort)>? fieldSelector,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(
            "GetPagedAsync with fieldSelector not implemented for this repository. Override in derived class if needed.");
    }
}

/// <summary>
/// Repository for entities with long Id
/// </summary>
public interface IRepository<T> : IRepository<T, long> where T : BaseEntity
{
}
