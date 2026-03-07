using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Common.Enums;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public sealed class TokenRepository : BaseRepository<Token, TokenId>, ITokenRepository
{
    public TokenRepository(IDbContext context) : base(context)
    {
    }

    public async Task<Token?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ReferenceId == referenceId, cancellationToken);
    }

    public async Task<Token?> GetByAuthorizationIdAsync(AuthorizationId authorizationId, string type,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(t => t.AuthorizationId == authorizationId && t.Type == type,
                cancellationToken);
    }

    public async Task RevokeAllByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(t => t.UserId == userId && t.Status == TokenStatus.Valid)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TokenStatus.Revoked), cancellationToken);
    }

    public async Task RevokeAllByAuthorizationIdAsync(AuthorizationId authorizationId,
        CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(t => t.AuthorizationId == authorizationId && t.Status == TokenStatus.Valid)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TokenStatus.Revoked), cancellationToken);
    }

    /// <summary>
    /// Deletes expired, redeemed, and revoked tokens for a user to prevent table bloat.
    /// Removes:
    ///   - Redeemed authorization codes (single-use, no longer needed)
    ///   - Revoked tokens
    ///   - Expired tokens (ExpirationDate in the past)
    ///   - Orphaned SSO tokens (ApplicationId=NULL) that are no longer valid
    /// </summary>
    public async Task DeleteExpiredAndRedeemedByUserAsync(UserId userId,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow;
        await DbSet
            .Where(t => t.UserId == userId &&
                        (t.Status == TokenStatus.Redeemed ||
                         t.Status == TokenStatus.Revoked ||
                         (t.ExpirationDate != null && t.ExpirationDate < cutoff)))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Token>> GetActiveSessionsByUserIdAsync(UserId userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await DbSet
            .AsNoTracking()
            .Where(t => t.UserId == userId &&
                        t.Type == OAuthConstants.TokenTypes.RefreshToken &&
                        t.Status == TokenStatus.Valid &&
                        (t.ExpirationDate == null || t.ExpirationDate > now))
            .OrderByDescending(t => t.CreationDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Token>> GetAllValidRefreshTokensByAuthorizationIdAsync(
        AuthorizationId authorizationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.AuthorizationId == authorizationId &&
                        t.Type == OAuthConstants.TokenTypes.RefreshToken &&
                        t.Status == TokenStatus.Valid)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Token?> GetLatestValidRefreshTokenByAuthorizationIdAsync(AuthorizationId authorizationId,
        DateTime createdAfter,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.AuthorizationId == authorizationId &&
                        t.Type == OAuthConstants.TokenTypes.RefreshToken &&
                        t.Status == TokenStatus.Valid &&
                        t.CreationDate > createdAfter)
            .OrderByDescending(t => t.CreationDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> RedeemByIdAsync(TokenId tokenId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await DbSet
            .Where(t => t.Id == tokenId && t.Status == TokenStatus.Valid)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Status, TokenStatus.Redeemed)
                    .SetProperty(t => t.RedemptionDate, now),
                cancellationToken);
    }
}
