using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Roles.Common;

namespace Alfred.Identity.Application.Roles;

public interface IRoleService
{
    Task<PageResult<RoleDto>> GetAllRolesAsync(QueryRequest query,
        CancellationToken cancellationToken = default);

    Task<RoleDto?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PermissionDto>> GetRolePermissionsAsync(Guid roleId,
        CancellationToken cancellationToken = default);

    Task<RoleDto> CreateRoleAsync(string name, string? icon, bool isImmutable, bool isSystem,
        IEnumerable<Guid>? permissions, CancellationToken cancellationToken = default);

    Task<RoleDto> UpdateRoleAsync(Guid id, string name, string? icon, bool isImmutable, bool isSystem,
        IEnumerable<Guid>? permissions, CancellationToken cancellationToken = default);

    Task<RoleDto> DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleDto> AddPermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default);

    Task<RoleDto> RemovePermissionsFromRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default);
}
