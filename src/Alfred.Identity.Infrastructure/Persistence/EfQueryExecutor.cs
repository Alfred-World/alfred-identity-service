using Alfred.Identity.Domain.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IAsyncQueryExecutor"/>.
/// Keeps EF extension methods (ToListAsync, AsNoTracking, etc.) confined to Infrastructure.
/// </summary>
public sealed class EfQueryExecutor : IAsyncQueryExecutor
{
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.ToListAsync(cancellationToken);

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.FirstOrDefaultAsync(cancellationToken);

    public Task<long> LongCountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.LongCountAsync(cancellationToken);

    public IQueryable<T> AsNoTracking<T>(IQueryable<T> query) where T : class
        => query.AsNoTracking();
}
