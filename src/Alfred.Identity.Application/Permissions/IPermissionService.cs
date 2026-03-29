using Alfred.Identity.Application.Permissions.Common;

namespace Alfred.Identity.Application.Permissions;

public interface IPermissionService
{
    Task<PageResult<PermissionDto>> GetAllPermissionsAsync(QueryRequest query, CancellationToken ct = default);
}
