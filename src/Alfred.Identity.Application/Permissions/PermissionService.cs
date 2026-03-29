using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Querying.Filtering.Parsing;

namespace Alfred.Identity.Application.Permissions;

public sealed class PermissionService : BaseEntityService, IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(
        IPermissionRepository permissionRepository,
        IFilterParser filterParser,
        IAsyncQueryExecutor executor)
        : base(filterParser, executor)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<PageResult<PermissionDto>> GetAllPermissionsAsync(QueryRequest query,
        CancellationToken ct = default)
    {
        return await GetPagedWithViewAsync(_permissionRepository, query, PermissionFieldMap.Instance,
            PermissionFieldMap.Views, p => PermissionDto.FromEntity(p), ct);
    }
}
