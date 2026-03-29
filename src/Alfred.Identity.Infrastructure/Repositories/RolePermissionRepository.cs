using Alfred.Identity.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly IDbContext _context;

    public RolePermissionRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByRoleNameAsync(string roleName,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoleName = roleName.ToUpperInvariant();
        return await _context.Set<RolePermission>()
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .Where(rp => rp.Role.NormalizedName == normalizedRoleName)
            .Select(rp => rp.Permission)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
    }
}
