using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.AddPermissions;

public class
    AddPermissionsToRoleCommandHandler : IRequestHandler<AddPermissionsToRoleCommand, AddPermissionsToRoleResult>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ICurrentUser _currentUser;

    public AddPermissionsToRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ICurrentUser currentUser)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _currentUser = currentUser;
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

        // Efficient N+1 fix: Fetch all requested permissions in one query
        var uniquePermissionIds = request.PermissionIds.Distinct().ToList();
        if (uniquePermissionIds.Any())
        {
            // Use base repository method to find by IDs
            var validPermissions =
                await _permissionRepository.FindAsync(p => uniquePermissionIds.Contains(p.Id), cancellationToken);
            var validPermissionIds = validPermissions.Select(p => p.Id).ToHashSet();

            // Check if all requested IDs are valid
            var invalidIds = uniquePermissionIds.Where(id => !validPermissionIds.Contains(id)).ToList();
            if (invalidIds.Any())
            {
                // Return explicitly which IDs were not found
                return new AddPermissionsToRoleResult(false, $"Permissions not found: {string.Join(", ", invalidIds)}");
            }
        }

        // Sync permissions with current user as creator
        role.SyncPermissions(uniquePermissionIds, _currentUser.UserId);

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return new AddPermissionsToRoleResult(true);
    }
}

