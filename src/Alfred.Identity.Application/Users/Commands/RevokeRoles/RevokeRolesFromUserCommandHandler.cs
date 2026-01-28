using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Users.Commands.RevokeRoles;

public class RevokeRolesFromUserCommandHandler : IRequestHandler<RevokeRolesFromUserCommand, RevokeRolesFromUserResult>
{
    private readonly IUserRepository _userRepository;

    public RevokeRolesFromUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<RevokeRolesFromUserResult> Handle(RevokeRolesFromUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return new RevokeRolesFromUserResult(false, "User not found");
        }

        foreach (var roleId in request.RoleIds)
        {
            user.RemoveRole(roleId);
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new RevokeRolesFromUserResult(true);
    }
}
