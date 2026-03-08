namespace Alfred.Identity.Domain.Abstractions;

/// <summary>
/// Abstraction over async query materialisation methods (ToListAsync, FirstOrDefaultAsync, etc.).
/// Defined in Domain so both Application (consumer) and Infrastructure (implementor) can depend on it
/// without creating an Infrastructure → Application circular reference.
/// </summary>
public interface IAsyncQueryExecutor
{
    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    Task<long> LongCountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the query with change-tracking disabled (EF: AsNoTracking).
    /// For non-EF providers this can be a no-op passthrough.
    /// </summary>
    IQueryable<T> AsNoTracking<T>(IQueryable<T> query) where T : class;
}
