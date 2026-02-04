using Alfred.Identity.Domain.Common.Interfaces;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IUserLoginRepository : IRepository<UserLogin>
{
    Task<UserLogin?> GetByProviderAndKeyAsync(string provider, string key, CancellationToken cancellationToken = default);
}
