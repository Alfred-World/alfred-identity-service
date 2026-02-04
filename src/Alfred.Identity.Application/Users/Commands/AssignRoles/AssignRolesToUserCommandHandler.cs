using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Users.Commands.AssignRoles;

public class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, AssignRolesToUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICurrentUser _currentUser;

    public AssignRolesToUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _currentUser = currentUser;
    }

    public async Task<AssignRolesToUserResult> Handle(AssignRolesToUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return new AssignRolesToUserResult(false, "User not found");
        }

        var currentUserId = _currentUser.UserId;

        foreach (var roleId in request.RoleIds)
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role == null)
            {
                return new AssignRolesToUserResult(false, $"Role with ID {roleId} not found");
            }

            user.AddRole(roleId, currentUserId);
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new AssignRolesToUserResult(true);
    }
}

