using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, UpdateRoleResult>
{
    private readonly IRoleRepository _roleRepository;

    public UpdateRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<UpdateRoleResult> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (role == null)
        {
            return new UpdateRoleResult(false, "Role not found.");
        }

        if (role.IsImmutable)
        {
            return new UpdateRoleResult(false, "Cannot modify immutable role.");
        }

        // Update using domain method
        role.Update(request.Name, request.Icon);

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return new UpdateRoleResult(true);
    }
}
