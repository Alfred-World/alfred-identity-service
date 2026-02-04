using Alfred.Identity.Application.Common;


using Alfred.Identity.Application.Users.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ExternalLogin;

public class LoginWithExternalProviderCommandHandler : IRequestHandler<LoginWithExternalProviderCommand, Result<LoginWithExternalProviderResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserLoginRepository _userLoginRepository;

    public LoginWithExternalProviderCommandHandler(IUserRepository userRepository, IUserLoginRepository userLoginRepository)
    {
        _userRepository = userRepository;
        _userLoginRepository = userLoginRepository;
    }

    public async Task<Result<LoginWithExternalProviderResult>> Handle(LoginWithExternalProviderCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if user exists via UserLogin
        var userLogin = await _userLoginRepository.GetByProviderAndKeyAsync(request.Provider, request.ProviderKey, cancellationToken);

        if (userLogin != null)
        {
            return Result<LoginWithExternalProviderResult>.Success(new LoginWithExternalProviderResult(
                UserDto.FromEntity(userLogin.User),
                false));
        }

        // 2. If not found, check by Email (if provided)
        User? user = null;
        if (!string.IsNullOrEmpty(request.Email))
        {
            user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        }

        bool isNewUser = false;
        if (user == null)
        {
            // 3. Create new user if not found
            var userName = !string.IsNullOrEmpty(request.Email)
                ? request.Email.Split('@')[0]
                : $"user_{Guid.NewGuid().ToString("N")[..8]}";

            // Ensure unique username
            while (await _userRepository.GetByUsernameAsync(userName, cancellationToken) != null)
            {
                userName = $"{userName}_{Guid.NewGuid().ToString("N")[..4]}";
            }


            user = User.CreateWithUsername(
                request.Email ?? $"{Guid.NewGuid()}@placeholder.com", // Fallback if no email
                userName,
                null, // No password
                request.DisplayName ?? request.Email ?? "Unknown",
                !string.IsNullOrEmpty(request.Email) // Email Confirmed
            );


            await _userRepository.AddAsync(user, cancellationToken);
            isNewUser = true;
        }

        // 4. Link UserLogin
        user.AddLogin(request.Provider, request.ProviderKey, request.DisplayName);

        // If existing user, we need to update to save the new Login
        if (!isNewUser)
        {
            _userRepository.Update(user);
        }

        await _userRepository.SaveChangesAsync(cancellationToken);

        // Reload user to get Roles/includes
        user = await _userRepository.GetByIdWithRolesAsync(user.Id, cancellationToken);

        return Result<LoginWithExternalProviderResult>.Success(new LoginWithExternalProviderResult(
            UserDto.FromEntity(user!),
            isNewUser));
    }
}


