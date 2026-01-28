using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Roles.Queries.GetRolePermissions;

public class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, List<PermissionDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRolePermissionsQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<List<PermissionDto>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return new List<PermissionDto>();
        }

        return role.RolePermissions
            .Select(rp => PermissionDto.FromEntity(rp.Permission))
            .ToList();
    }
}
