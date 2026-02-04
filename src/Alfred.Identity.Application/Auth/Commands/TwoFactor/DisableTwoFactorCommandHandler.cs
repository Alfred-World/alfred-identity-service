using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.TwoFactor;

public class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;

    public DisableTwoFactorCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure("User not found");
        }

        user.DisableTwoFactor();
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
