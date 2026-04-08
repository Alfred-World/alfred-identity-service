using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Application.Permissions;

public interface IPermissionService
{
    Task<PageResult<PermissionDto>> SearchPermissionsAsync(SearchRequest request, CancellationToken ct = default);
    SearchMetadataResponse GetSearchMetadata();
}
