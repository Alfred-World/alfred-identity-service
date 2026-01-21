using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface ISigningKeyRepository : IRepository<SigningKey>
{
    Task<SigningKey?> GetActiveKeyAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SigningKey>> GetValidKeysAsync(CancellationToken cancellationToken = default);
}
