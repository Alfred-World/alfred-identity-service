using Alfred.Identity.Domain.Abstractions.Services;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUserReplicationEventPublisher _replicationEventPublisher;

    public UpdateProfileCommandHandler(
        IUserRepository userRepository,
        IIdentityUserReplicationEventPublisher replicationEventPublisher)
    {
        _userRepository = userRepository;
        _replicationEventPublisher = replicationEventPublisher;
    }

    public async Task<Result<UpdateProfileResult>> Handle(UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<UpdateProfileResult>.Failure("UserNotFound");
        }

        user.UpdateProfile(request.FullName, request.PhoneNumber);

        if (request.Avatar != null)
        {
            user.UpdateAvatar(request.Avatar);
        }

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
        await _replicationEventPublisher.PublishUserUpsertedAsync(
            user.Id.Value,
            user.UserName,
            user.Email,
            user.FullName,
            user.Avatar,
            user.Status.ToString(),
            user.IsBanned,
            user.IsDeleted,
            cancellationToken);

        return Result<UpdateProfileResult>.Success(new UpdateProfileResult(
            user.Id.Value,
            user.FullName,
            user.PhoneNumber,
            user.Avatar,
            user.Email,
            user.UserName
        ));
    }
}
