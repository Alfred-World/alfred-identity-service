using System.Linq.Expressions;

using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

/// <summary>
/// Permission repository implementation
/// </summary>
public class PermissionRepository : IPermissionRepository
{
    private readonly PostgreSqlDbContext _context;

    public PermissionRepository(PostgreSqlDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> FindAsync(Expression<Func<Permission, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Permission entity, CancellationToken cancellationToken = default)
    {
        await _context.Permissions.AddAsync(entity, cancellationToken);
    }

    public void Update(Permission entity)
    {
        _context.Permissions.Update(entity);
    }

    public void Delete(Permission entity)
    {
        _context.Permissions.Remove(entity);
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.AnyAsync(p => p.Id == id, cancellationToken);
    }

    public IQueryable<Permission> GetQueryable()
    {
        return _context.Permissions.AsQueryable();
    }

    // Custom methods
    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant();
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.Code == normalizedCode, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByResourceAsync(string resource, CancellationToken cancellationToken = default)
    {
        var normalizedResource = resource.ToLowerInvariant();
        return await _context.Permissions
            .Where(p => p.Resource == normalizedResource)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Permissions
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant();
        return await _context.Permissions.AnyAsync(p => p.Code == normalizedCode, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
