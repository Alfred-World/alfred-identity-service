using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

/// <summary>
/// Permission repository implementation
/// </summary>
public class PermissionRepository : Repository<Permission>, IPermissionRepository
{
    public PermissionRepository(IDbContext context) : base(context)
    {
    }

    // Custom methods
    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant();
        return await DbSet
            .FirstOrDefaultAsync(p => p.Code == normalizedCode, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByResourceAsync(string resource,
        CancellationToken cancellationToken = default)
    {
        var normalizedResource = resource.ToLowerInvariant();
        return await DbSet
            .Where(p => p.Resource == normalizedResource)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant();
        return await DbSet.AnyAsync(p => p.Code == normalizedCode, cancellationToken);
    }
}
