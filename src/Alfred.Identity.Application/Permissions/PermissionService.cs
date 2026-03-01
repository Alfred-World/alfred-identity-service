using Alfred.Identity.Application.Common;
using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Querying.Filtering.Parsing;
using Alfred.Identity.Domain.Abstractions.Repositories;

namespace Alfred.Identity.Application.Permissions;

public sealed class PermissionService : BaseEntityService, IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(IPermissionRepository permissionRepository, IFilterParser filterParser)
        : base(filterParser)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<PageResult<PermissionDto>> GetAllPermissionsAsync(QueryRequest query,
        CancellationToken ct = default)
    {
        return await GetPagedAsync(_permissionRepository, query, PermissionFieldMap.Instance,
            p => PermissionDto.FromEntity(p), ct);
    }
}
