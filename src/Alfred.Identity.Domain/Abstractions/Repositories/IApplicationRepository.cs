using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IApplicationRepository : IRepository<Application>
{
    Task<Application?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}
