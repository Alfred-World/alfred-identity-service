using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IAuthorizationRepository : IRepository<Authorization, AuthorizationId>
{
    Task<Authorization?> GetValidAsync(ApplicationId applicationId, UserId userId, string scopes,
        CancellationToken cancellationToken = default);
}
