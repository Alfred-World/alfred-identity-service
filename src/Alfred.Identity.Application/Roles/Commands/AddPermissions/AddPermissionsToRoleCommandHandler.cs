using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.AddPermissions;

public class
    AddPermissionsToRoleCommandHandler : IRequestHandler<AddPermissionsToRoleCommand, AddPermissionsToRoleResult>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public AddPermissionsToRoleCommandHandler(IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<AddPermissionsToRoleResult> Handle(AddPermissionsToRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return new AddPermissionsToRoleResult(false, "Role not found");
        }

        if (role.IsImmutable)
        {
            return new AddPermissionsToRoleResult(false, "Cannot modify immutable role");
        }

        foreach (var permissionId in request.PermissionIds)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
            if (permission == null)
            {
                return new AddPermissionsToRoleResult(false, $"Permission with ID {permissionId} not found");
            }

            role.AddPermission(permissionId);
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return new AddPermissionsToRoleResult(true);
    }
}
