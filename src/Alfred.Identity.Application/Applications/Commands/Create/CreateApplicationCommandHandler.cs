using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;

using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.Create;

public class CreateApplicationCommandHandler : IRequestHandler<CreateApplicationCommand, long>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IPasswordHasher _passwordHasher; // Use if we hash secrets
    private readonly IUnitOfWork _unitOfWork; // Or use repo.SaveChangesAsync

    public CreateApplicationCommandHandler(
        IApplicationRepository applicationRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _applicationRepository = applicationRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _applicationRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException("Client ID already exists");
        }

        // Only hash secret if confidential
        // For public clients, secret might be empty or mocked.
        // Assuming we store plain or hashed? OpenIddict recommends hashing.
        // Let's use our PasswordHasher for consistency, though ideally different generic hasher.
        // Or store plain if we don't have hashing mechanism for non-user secrets yet.
        // Let's assume plain for simplicity now, or Hash if non-empty.
        string? secretHash = null;
        if (!string.IsNullOrEmpty(request.ClientSecret))
        {
            secretHash = _passwordHasher.HashPassword(request.ClientSecret);
            // Note: PasswordHasher is for Users, verifies against hash. 
            // Works for secrets too.
        }

        var app = Domain.Entities.Application.Create(
            request.ClientId,
            clientSecret: secretHash, // Storing HASHED secret
            displayName: request.DisplayName,
            redirectUris: request.RedirectUris,
            postLogoutRedirectUris: request
                .PostLogoutRedirectUris, // Assuming this is correct or I update it based on view
            permissions: request.Permissions,
            clientType: request.Type
        );

        await _applicationRepository.AddAsync(app, cancellationToken);

        // Use UnitOfWork to save? Repos usually don't have SaveChanges in standard DDD unless Repository=UoW.
        // But previously I added SaveChangesAsync to AuthorizationRepository.
        // IApplicationRepository doesn't have it yet? I should check IApplicationRepository.
        // Or use IUnitOfWork.SaveChangesAsync().
        // Let's use IUnitOfWork.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return app.Id;
    }
}
