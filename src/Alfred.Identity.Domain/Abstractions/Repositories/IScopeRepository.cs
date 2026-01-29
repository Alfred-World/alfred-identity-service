using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IScopeRepository : IRepository<Scope>
{
    Task<Scope?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Scope>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
