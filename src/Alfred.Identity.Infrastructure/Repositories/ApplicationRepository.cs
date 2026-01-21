using System.Linq.Expressions;

using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly PostgreSqlDbContext _context;

    public ApplicationRepository(PostgreSqlDbContext context)
    {
        _context = context;
    }

    public async Task<Application?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Applications.FindAsync([id], cancellationToken);
    }

    public async Task<Application?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Applications
            .FirstOrDefaultAsync(a => a.ClientId == clientId, cancellationToken);
    }

    public async Task<IEnumerable<Application>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Applications.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Application>> FindAsync(Expression<Func<Application, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Applications.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Application entity, CancellationToken cancellationToken = default)
    {
        await _context.Applications.AddAsync(entity, cancellationToken);
    }

    public void Update(Application entity)
    {
        _context.Applications.Update(entity);
    }

    public void Delete(Application entity)
    {
        _context.Applications.Remove(entity);
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Applications.AnyAsync(a => a.Id == id, cancellationToken);
    }

    public IQueryable<Application> GetQueryable()
    {
        return _context.Applications.AsQueryable();
    }
}
