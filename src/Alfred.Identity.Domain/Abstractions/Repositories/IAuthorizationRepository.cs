using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IAuthorizationRepository : IRepository<Authorization>
{
    // Add specific methods if needed, e.g.
    Task<Authorization?> GetValidAsync(long applicationId, long userId, string scopes, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
