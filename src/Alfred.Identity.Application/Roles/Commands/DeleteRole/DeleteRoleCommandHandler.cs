using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.DeleteRole;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, DeleteRoleResult>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser;

    public DeleteRoleCommandHandler(IRoleRepository roleRepository, ICurrentUser currentUser)
    {
        _roleRepository = roleRepository;
        _currentUser = currentUser;
    }

    public async Task<DeleteRoleResult> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (role == null)
        {
            return new DeleteRoleResult(false, Error: "Role not found.");
        }

        if (role.IsImmutable)
        {
            return new DeleteRoleResult(false, Error: "Cannot delete immutable role.");
        }

        if (role.IsSystem)
        {
            return new DeleteRoleResult(false, Error: "Cannot delete system role.");
        }

        var deletedRoleDto = RoleDto.FromEntity(role);

        // Set DeletedById before soft delete
        role.DeletedById = _currentUser.UserId;

        await _roleRepository.DeleteAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return new DeleteRoleResult(true, deletedRoleDto);
    }
}

