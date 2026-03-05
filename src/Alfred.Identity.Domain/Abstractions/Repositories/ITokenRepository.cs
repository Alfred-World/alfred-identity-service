using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

/// <summary>
/// Repository for managing OAuth2/OpenID Connect tokens
/// </summary>
public interface ITokenRepository : IRepository<Token>
{
    Task<Token?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);

    Task<Token?> GetByAuthorizationIdAsync(Guid authorizationId, string type,
        CancellationToken cancellationToken = default);

    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeAllByAuthorizationIdAsync(Guid authorizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all Valid refresh tokens for a given authorization (session).
    /// Used to revoke duplicates before rotation.
    /// </summary>
    Task<IReadOnlyList<Token>> GetAllValidRefreshTokensByAuthorizationIdAsync(Guid authorizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the newest Valid refresh token created for an authorization after a given UTC timestamp.
    /// Used in grace-period logic: if original RT was just redeemed and a new one already exists, return it.
    /// </summary>
    Task<Token?> GetLatestValidRefreshTokenByAuthorizationIdAsync(Guid authorizationId, DateTime createdAfter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired, redeemed, or revoked tokens for a given user.
    /// Called at SSO login time to clean up orphaned rows from previous failed/completed flows.
    /// </summary>
    Task DeleteExpiredAndRedeemedByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a single token as Redeemed using a direct UPDATE (bypasses EF change tracking).
    /// This avoids DbUpdateConcurrencyException when parallel requests race to redeem the same token.
    /// Returns the number of rows affected (0 if the token was already deleted/redeemed).
    /// </summary>
    Task<int> RedeemByIdAsync(Guid tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all valid (non-expired, non-revoked) refresh_token sessions for a user,
    /// used in the Recent Devices / Active Sessions view.
    /// </summary>
    Task<IReadOnlyList<Token>> GetActiveSessionsByUserIdAsync(Guid userId,
        CancellationToken cancellationToken = default);
}
