using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Users.Commands.AssignRoles;

public class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, AssignRolesToUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public AssignRolesToUserCommandHandler(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<AssignRolesToUserResult> Handle(AssignRolesToUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return new AssignRolesToUserResult(false, "User not found");
        }

        foreach (var roleId in request.RoleIds)
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role == null)
            {
                return new AssignRolesToUserResult(false, $"Role with ID {roleId} not found");
            }

            user.AddRole(roleId);
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new AssignRolesToUserResult(true);
    }
}
