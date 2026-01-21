using System.Linq.Expressions;

using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public class AuthorizationRepository : IAuthorizationRepository
{
    private readonly PostgreSqlDbContext _context;

    public AuthorizationRepository(PostgreSqlDbContext context)
    {
        _context = context;
    }

    public async Task<Authorization?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Authorizations.FindAsync([id], cancellationToken);
    }

    public async Task<Authorization?> GetValidAsync(long applicationId, long userId, string scopes,
        CancellationToken cancellationToken = default)
    {
        // Simple exact match or subset logic? For now assume exact string match or simplified
        return await _context.Authorizations
            .FirstOrDefaultAsync(a =>
                    a.ApplicationId == applicationId &&
                    a.UserId == userId &&
                    a.Status == "Valid" &&
                    a.Scopes == scopes,
                cancellationToken);
    }

    public async Task<IEnumerable<Authorization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Authorizations.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Authorization>> FindAsync(Expression<Func<Authorization, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Authorizations.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Authorization entity, CancellationToken cancellationToken = default)
    {
        await _context.Authorizations.AddAsync(entity, cancellationToken);
    }

    public void Update(Authorization entity)
    {
        _context.Authorizations.Update(entity);
    }

    public void Delete(Authorization entity)
    {
        _context.Authorizations.Remove(entity);
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Authorizations.AnyAsync(a => a.Id == id, cancellationToken);
    }

    public IQueryable<Authorization> GetQueryable()
    {
        return _context.Authorizations.AsQueryable();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
