using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

namespace Alfred.Identity.Infrastructure.Repositories;

public class PermissionRepository : BaseRepository<Permission, PermissionId>, IPermissionRepository
{
    public PermissionRepository(IDbContext context) : base(context)
    {
    }
}
