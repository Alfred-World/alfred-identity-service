using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public class SigningKeyRepository : Repository<SigningKey>, ISigningKeyRepository
{
    public SigningKeyRepository(IDbContext context) : base(context)
    {
    }

    // ISigningKeyRepository specific methods
    public async Task<SigningKey?> GetActiveKeyAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(k => k.IsActive && (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow))
            .OrderByDescending(k => k.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<SigningKey>> GetValidKeysAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(k => k.IsActive)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
