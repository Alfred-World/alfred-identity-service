using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Common.Enums;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public sealed class TokenRepository : Repository<Token>, ITokenRepository
{
    public TokenRepository(IDbContext context) : base(context)
    {
    }

    public async Task<Token?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ReferenceId == referenceId, cancellationToken);
    }

    public async Task<Token?> GetByAuthorizationIdAsync(long authorizationId, string type,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(t => t.AuthorizationId == authorizationId && t.Type == type,
                cancellationToken);
    }

    public async Task RevokeAllByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(t => t.UserId == userId && t.Status == TokenStatus.Valid)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TokenStatus.Revoked), cancellationToken);
    }

    public async Task RevokeAllByAuthorizationIdAsync(long authorizationId,
        CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(t => t.AuthorizationId == authorizationId && t.Status == TokenStatus.Valid)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TokenStatus.Revoked), cancellationToken);
    }
}
