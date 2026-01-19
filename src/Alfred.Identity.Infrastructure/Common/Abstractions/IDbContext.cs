using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Alfred.Identity.Infrastructure.Common.Abstractions;

/// <summary>
/// Abstraction for DbContext to support multiple database providers
/// Allows easy switching between SQL Server, PostgreSQL, MySQL, etc.
/// </summary>
public interface IDbContext : IDisposable
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    ChangeTracker ChangeTracker { get; }
    DatabaseFacade Database { get; }
}
