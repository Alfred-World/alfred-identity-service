using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;
using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure("User not found");
        }

        if (!user.HasPassword())
        {
            return Result<bool>.Failure("User does not have a password set. Use Reset Password flow.");
        }

        if (!_passwordHasher.VerifyPassword(request.OldPassword, user.PasswordHash!))
        {
            return Result<bool>.Failure("Incorrect old password");
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SetPassword(newPasswordHash);

        await _userRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
