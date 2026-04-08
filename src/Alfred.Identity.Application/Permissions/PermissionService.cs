using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Application.Permissions;

public sealed class PermissionService : BaseEntityService, IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IAsyncQueryExecutor executor)
        : base(executor)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<PageResult<PermissionDto>> SearchPermissionsAsync(SearchRequest request,
        CancellationToken ct = default)
    {
        return await SearchWithViewAsync(_permissionRepository, request, PermissionFieldMap.Instance,
            PermissionFieldMap.Views, p => PermissionDto.FromEntity(p), ct);
    }

    public SearchMetadataResponse GetSearchMetadata()
    {
        return BuildSearchMetadata(PermissionFieldMap.Instance);
    }
}
