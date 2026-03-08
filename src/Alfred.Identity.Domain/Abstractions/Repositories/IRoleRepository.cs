using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IRoleRepository : IRepository<Role, RoleId>
{
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
