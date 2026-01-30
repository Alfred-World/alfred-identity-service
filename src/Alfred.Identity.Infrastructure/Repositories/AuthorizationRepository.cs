using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public class AuthorizationRepository : BaseRepository<Authorization>, IAuthorizationRepository
{
    public AuthorizationRepository(IDbContext context) : base(context)
    {
    }

    public async Task<Authorization?> GetValidAsync(long applicationId, long userId, string scopes,
        CancellationToken cancellationToken = default)
    {
        // Simple exact match or subset logic? For now assume exact string match or simplified
        return await DbSet
            .FirstOrDefaultAsync(a =>
                    a.ApplicationId == applicationId &&
                    a.UserId == userId &&
                    a.Status == "Valid" &&
                    a.Scopes == scopes,
                cancellationToken);
    }
}
