using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IApplicationRepository : IRepository<Application, ApplicationId>
{
    Task<Application?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}
