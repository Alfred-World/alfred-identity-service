using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.RemovePermissions;

public class
    RemovePermissionsFromRoleCommandHandler : IRequestHandler<RemovePermissionsFromRoleCommand,
    RemovePermissionsFromRoleResult>
{
    private readonly IRoleRepository _roleRepository;

    public RemovePermissionsFromRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RemovePermissionsFromRoleResult> Handle(RemovePermissionsFromRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role == null)
        {
            return new RemovePermissionsFromRoleResult(false, "Role not found");
        }

        if (role.IsImmutable)
        {
            return new RemovePermissionsFromRoleResult(false, "Cannot modify immutable role");
        }

        foreach (var permissionId in request.PermissionIds)
        {
            role.RemovePermission(permissionId);
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return new RemovePermissionsFromRoleResult(true);
    }
}
