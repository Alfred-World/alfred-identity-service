using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<UpdateProfileResult>>
{
    private readonly IUserRepository _userRepository;

    public UpdateProfileCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
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
