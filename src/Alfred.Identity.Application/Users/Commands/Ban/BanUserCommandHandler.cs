using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Users.Commands.Ban;

public class BanUserCommandHandler : IRequestHandler<BanUserCommand, BanUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUserActivityLogger _activityLogger;

    public BanUserCommandHandler(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IUserActivityLogger activityLogger)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _activityLogger = activityLogger;
    }

    public async Task<BanUserResult> Handle(BanUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return new BanUserResult(false, "User not found");
        }

        if (user.IsBanned)
        {
            return new BanUserResult(false, "User is already banned");
        }

        // Ban logic
        user.Ban(request.Reason, _currentUser.UserId, request.ExpiresAt);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);


        // Log activity
        await _activityLogger.LogAsync(
            request.UserId,
            "BanUser",
            $"Banned by {_currentUser.Username}. Reason: {request.Reason}",
            cancellationToken);

        return new BanUserResult(true);
    }
}
