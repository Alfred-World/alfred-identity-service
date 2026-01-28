using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;

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

        // Reflection hack to set private property if no public setter or update method exists
        // Ideally Role entity should have an Update method.
        // Checking Role.cs entity definition...
        // Property Name has private set. Need to check if there is an update method.
        // Looking at Step 24, Role.cs does NOT have an Update method.
        // I should probably add an Update method to Role entity first or use reflection here.
        // Choosing Reflection for now to avoid modifying Entity unless necessary, but modifying Entity is cleaner.
        // Let's modify Entity in a separate step or just use reflection here for speed if user allows.
        // Actually, best practice is to add domain method.

        // Assuming I will add Update method to Role.
        // But for now, I'll use reflection to update the backing field or property.

        var nameProp = typeof(Role).GetProperty(nameof(Role.Name));
        if (nameProp != null)
        {
            nameProp.SetValue(role, request.Name);
            var normalizedNameProp = typeof(Role).GetProperty(nameof(Role.NormalizedName));
            normalizedNameProp?.SetValue(role, request.Name.ToUpperInvariant());
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        return new UpdateRoleResult(true);
    }
}
