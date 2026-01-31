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

}
