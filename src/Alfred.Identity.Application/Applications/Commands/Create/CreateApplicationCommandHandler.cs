using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;

using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.Create;

public class CreateApplicationCommandHandler : IRequestHandler<CreateApplicationCommand, CreateApplicationResult>
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

    public async Task<CreateApplicationResult> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _applicationRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException("Client ID already exists");
        }
        string? secretHash = null;
        string? rawSecret = null;
        if (request.Type == "confidential")
        {
            // Generate a secure random secret (32 bytes -> base64)
            // Note: The UI won't see this. User must regenerate to see it.
            var secretBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
            rawSecret = Convert.ToBase64String(secretBytes);
            secretHash = _passwordHasher.HashPassword(rawSecret);
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

        // Return result with raw secret if available
        return new CreateApplicationResult(app.Id, request.Type == "confidential" ? rawSecret : null);
    }
}
