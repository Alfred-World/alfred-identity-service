using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IAuthorizationRepository : IRepository<Authorization>
{
    // Add specific methods if needed, e.g.
    Task<Authorization?> GetValidAsync(Guid applicationId, Guid userId, string scopes,
        CancellationToken cancellationToken = default);


}
