using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.CreateRole;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, CreateRoleResult>
{
    private readonly IRoleRepository _roleRepository;

    public CreateRoleCommandHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<CreateRoleResult> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await _roleRepository.ExistsAsync(request.Name, cancellationToken))
        {
            return new CreateRoleResult(false, Error: $"Role '{request.Name}' already exists.");
        }

        var role = Role.Create(
            request.Name,
            request.Icon,
            request.IsImmutable,
            request.IsSystem);

        if (request.Permissions != null && request.Permissions.Any())
        {
            foreach (var permissionId in request.Permissions)
            {
                role.AddPermission(permissionId);
            }
        }

        await _roleRepository.AddAsync(role, cancellationToken);
        await _roleRepository.SaveChangesAsync(cancellationToken);

        // Load permissions for DTO
        var createdRole = await _roleRepository.GetByIdAsync(role.Id, cancellationToken);
        return new CreateRoleResult(true, role.Id, RoleDto.FromEntity(createdRole!));
    }
}
