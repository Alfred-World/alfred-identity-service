using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

/// <summary>
/// Repository for managing OAuth2/OpenID Connect tokens
/// </summary>
public interface ITokenRepository : IRepository<Token, TokenId>
{
    Task<Token?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);

    Task<Token?> GetByAuthorizationIdAsync(AuthorizationId authorizationId, string type,
        CancellationToken cancellationToken = default);

    Task RevokeAllByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    Task RevokeAllByAuthorizationIdAsync(AuthorizationId authorizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Token>> GetAllValidRefreshTokensByAuthorizationIdAsync(AuthorizationId authorizationId,
        CancellationToken cancellationToken = default);

    Task<Token?> GetLatestValidRefreshTokenByAuthorizationIdAsync(AuthorizationId authorizationId,
        DateTime createdAfter, CancellationToken cancellationToken = default);

    Task DeleteExpiredAndRedeemedByUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<int> RedeemByIdAsync(TokenId tokenId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Token>> GetActiveSessionsByUserIdAsync(UserId userId,
        CancellationToken cancellationToken = default);
}
