using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public sealed class ScopeRepository : BaseRepository<Scope>, IScopeRepository
{
    public ScopeRepository(IDbContext context) : base(context)
    {
    }

    public async Task<Scope?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<Scope>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        // For now there is no IsActive on Scope, let's just return all
        return await DbSet.ToListAsync(cancellationToken);
    }
}
