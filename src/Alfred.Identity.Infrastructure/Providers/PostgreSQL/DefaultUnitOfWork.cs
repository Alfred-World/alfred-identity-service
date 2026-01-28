using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL;

/// <summary>
/// Default implementation of Unit of Work pattern
/// Database-agnostic - works with any IDbContext implementation
/// </summary>
public class DefaultUnitOfWork : IUnitOfWork
{
    private readonly IDbContext _context;
    private IUserRepository? _users;
    private ITokenRepository? _tokens;

    public DefaultUnitOfWork(IDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public ITokenRepository Tokens =>
        _tokens ??= new TokenRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_context is DbContext dbContext)
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }

        throw new InvalidOperationException("DbContext is not available");
    }

    public void Dispose()
    {
        if (_context is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
