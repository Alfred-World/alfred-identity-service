using Alfred.Identity.Domain.Abstractions.Repositories;

namespace Alfred.Identity.Domain.Abstractions;

/// <summary>
/// Unit of Work pattern - manages transactions and repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Identity repositories
    IUserRepository Users { get; }
    ITokenRepository Tokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
