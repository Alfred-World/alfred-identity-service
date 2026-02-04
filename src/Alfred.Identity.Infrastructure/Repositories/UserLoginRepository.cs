using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public class UserLoginRepository : BaseRepository<UserLogin>, IUserLoginRepository
{
    public UserLoginRepository(IDbContext context) : base(context)
    {
    }

    public async Task<UserLogin?> GetByProviderAndKeyAsync(string provider, string key, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(ul => ul.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(ul => ul.LoginProvider == provider && ul.ProviderKey == key, cancellationToken);
    }
}
