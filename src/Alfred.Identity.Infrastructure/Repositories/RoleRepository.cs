using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(IDbContext context) : base(context)
    {
    }

    public override async Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.NormalizedName == name.ToUpperInvariant(), cancellationToken);
    }

    public override async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        DbSet.Update(role);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(role);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(r => r.NormalizedName == name.ToUpperInvariant(), cancellationToken);
    }
}
