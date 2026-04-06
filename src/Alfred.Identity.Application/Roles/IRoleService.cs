using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Roles.Common;

namespace Alfred.Identity.Application.Roles;

public interface IRoleService
{
    Task<PageResult<RoleDto>> GetAllRolesAsync(QueryRequest query,
        CancellationToken cancellationToken = default);

    Task<RoleDto?> GetRoleByIdAsync(RoleId id, CancellationToken cancellationToken = default);

    Task<List<PermissionDto>> GetRolePermissionsAsync(RoleId roleId,
        CancellationToken cancellationToken = default);

    Task<RoleDto> CreateRoleAsync(string name, string? icon, bool isImmutable, bool isSystem,
        IEnumerable<Guid>? permissions, CancellationToken cancellationToken = default);

    Task<RoleDto> UpdateRoleAsync(RoleId id, UpdateRoleDto dto, CancellationToken cancellationToken = default);

    Task<RoleDto> DeleteRoleAsync(RoleId id, CancellationToken cancellationToken = default);

    Task<RoleDto> AddPermissionsToRoleAsync(RoleId roleId, IEnumerable<PermissionId> permissionIds,
        CancellationToken cancellationToken = default);

    Task<RoleDto> RemovePermissionsFromRoleAsync(RoleId roleId, IEnumerable<PermissionId> permissionIds,
        CancellationToken cancellationToken = default);
}
