using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
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
    private readonly IIdentityUserReplicationEventPublisher _replicationEventPublisher;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IIdentityUserReplicationEventPublisher replicationEventPublisher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _replicationEventPublisher = replicationEventPublisher;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return new RegisterUserResult(false, Error: "Email already registered");
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user
        var user = User.Create(request.Email, passwordHash, request.FullName);

        // Save user
        await _userRepository.AddAsync(user, cancellationToken);
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

        return new RegisterUserResult(true, user.Id.Value);
    }
}
