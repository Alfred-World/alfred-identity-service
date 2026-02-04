using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, UpdateRoleResult>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser;

    public UpdateRoleCommandHandler(IRoleRepository roleRepository, ICurrentUser currentUser)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
    }

    public async Task<UpdateRoleResult> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (role == null)
        {
            return new UpdateRoleResult(false, Error: "Role not found.");
        }

        if (role.IsImmutable)
        {
            return new UpdateRoleResult(false, Error: "Cannot modify immutable role.");
        }

        var currentUserId = _currentUser.UserId;

        // Update using domain method
        role.Update(request.Name, request.Icon, request.IsImmutable, request.IsSystem);
        role.UpdatedById = currentUserId;

        if (request.Permissions != null)
        {
            role.SyncPermissions(request.Permissions, currentUserId);
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        // Load permissions for DTO
        var updatedRole = await _roleRepository.GetByIdAsync(role.Id, cancellationToken);
        return new UpdateRoleResult(true, RoleDto.FromEntity(updatedRole!));
    }
}

