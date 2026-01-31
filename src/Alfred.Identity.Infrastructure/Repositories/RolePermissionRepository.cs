using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

/// <summary>
/// RolePermission repository implementation for managing role-permission mappings
/// </summary>
public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly IDbContext _context;

    public RolePermissionRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<RolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);
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

    public async Task<IEnumerable<Role>> GetRolesByPermissionIdAsync(Guid permissionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<RolePermission>()
            .Where(rp => rp.PermissionId == permissionId)
            .Include(rp => rp.Role)
            .Select(rp => rp.Role)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
    {
        await _context.Set<RolePermission>().AddAsync(rolePermission, cancellationToken);
    }

    public void Remove(RolePermission rolePermission)
    {
        _context.Set<RolePermission>().Remove(rolePermission);
    }

    public async Task<bool> ExistsAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RolePermission>()
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
    }

    public async Task<RolePermission?> GetAsync(Guid roleId, Guid permissionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<RolePermission>()
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
