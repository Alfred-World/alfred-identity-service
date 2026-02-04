using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.TwoFactor;

public class InitiateEnableTwoFactorCommandHandler : IRequestHandler<InitiateEnableTwoFactorCommand, Result<InitiateEnableTwoFactorResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;

    public InitiateEnableTwoFactorCommandHandler(IUserRepository userRepository, ITwoFactorService twoFactorService)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
    }

    public async Task<Result<InitiateEnableTwoFactorResult>> Handle(InitiateEnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<InitiateEnableTwoFactorResult>.Failure("User not found");
        }

        if (user.TwoFactorEnabled)
        {
            return Result<InitiateEnableTwoFactorResult>.Failure("Two-factor authentication is already enabled.");
        }

        // Generate and store secret
        var secret = _twoFactorService.GenerateSecret();
        user.SetTwoFactorSecret(secret);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var uri = _twoFactorService.GenerateQrCodeUri(request.Email, secret);

        return Result<InitiateEnableTwoFactorResult>.Success(new InitiateEnableTwoFactorResult(secret, uri));
    }
}
