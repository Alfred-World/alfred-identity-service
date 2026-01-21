using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Alfred.Identity.Infrastructure.Repositories;

public class SigningKeyRepository : ISigningKeyRepository
{
    private readonly PostgreSqlDbContext _context;

    public SigningKeyRepository(PostgreSqlDbContext context)
    {
        _context = context;
    }

    // ISigningKeyRepository specific methods
    public async Task<SigningKey?> GetActiveKeyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SigningKeys
            .Where(k => k.IsActive && (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<SigningKey>> GetValidKeysAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SigningKeys
            .Where(k => k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // IRepository implementation
    public async Task<SigningKey?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.SigningKeys.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddAsync(SigningKey entity, CancellationToken cancellationToken = default)
    {
        await _context.SigningKeys.AddAsync(entity, cancellationToken);
    }

    public void Update(SigningKey entity)
    {
        _context.SigningKeys.Update(entity);
    }

    public void Delete(SigningKey entity)
    {
        _context.SigningKeys.Remove(entity);
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.SigningKeys.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<SigningKey>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SigningKeys.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SigningKey>> FindAsync(Expression<Func<SigningKey, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _context.SigningKeys.Where(predicate).ToListAsync(cancellationToken);
    }

    public IQueryable<SigningKey> GetQueryable()
    {
        return _context.SigningKeys.AsQueryable();
    }
}
