using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Register;

/// <summary>
/// Handler for RegisterUserCommand
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return new RegisterUserResult(false, Error: "Email already registered");
        }

        // Validate password complexity
        var passwordError = ValidatePasswordComplexity(request.Password);
        if (passwordError != null)
        {
            return new RegisterUserResult(false, Error: passwordError);
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user
        var user = User.Create(request.Email, passwordHash, request.FullName);

        // Save user
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new RegisterUserResult(true, user.Id);
    }

    /// <summary>
    /// Validates password complexity requirements
    /// </summary>
    private static string? ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required";
        }

        if (password.Length < 8)
        {
            return "Password must be at least 8 characters";
        }

        if (!password.Any(char.IsUpper))
        {
            return "Password must contain at least one uppercase letter";
        }

        if (!password.Any(char.IsLower))
        {
            return "Password must contain at least one lowercase letter";
        }

        if (!password.Any(char.IsDigit))
        {
            return "Password must contain at least one digit";
        }

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            return "Password must contain at least one special character";
        }

        return null;
    }
}
