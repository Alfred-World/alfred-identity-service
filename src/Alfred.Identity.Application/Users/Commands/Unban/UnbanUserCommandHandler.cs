using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Users.Commands.Unban;

public class UnbanUserCommandHandler : IRequestHandler<UnbanUserCommand, UnbanUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUserActivityLogger _activityLogger;

    public UnbanUserCommandHandler(
        IUserRepository userRepository, 
        ICurrentUser currentUser,
        IUserActivityLogger activityLogger)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _activityLogger = activityLogger;
    }

    public async Task<UnbanUserResult> Handle(UnbanUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return new UnbanUserResult(false, "User not found");
        }

        if (!user.IsBanned)
        {
            return new UnbanUserResult(false, "User is not banned");
        }

        // Unban logic
        user.Unban(_currentUser.UserId);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);


        // Log activity
        await _activityLogger.LogAsync(
            request.UserId, 
            "UnbanUser", 
            $"Unbanned by {_currentUser.Username}", 
            cancellationToken);

        return new UnbanUserResult(true);
    }
}
