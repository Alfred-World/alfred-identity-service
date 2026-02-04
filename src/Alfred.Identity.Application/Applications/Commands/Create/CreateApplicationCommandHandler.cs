using System.Security.Cryptography;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;

using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.Create;

public class CreateApplicationCommandHandler : IRequestHandler<CreateApplicationCommand, CreateApplicationResult>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateApplicationCommandHandler(
        IApplicationRepository applicationRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _applicationRepository = applicationRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateApplicationResult> Handle(CreateApplicationCommand request,
        CancellationToken cancellationToken)
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
            var secretBytes = RandomNumberGenerator.GetBytes(32);
            rawSecret = Convert.ToBase64String(secretBytes);
            secretHash = _passwordHasher.HashPassword(rawSecret);
        }

        var app = Domain.Entities.Application.Create(
            request.ClientId,
            clientSecret: secretHash,
            displayName: request.DisplayName,
            redirectUris: request.RedirectUris,
            postLogoutRedirectUris: request.PostLogoutRedirectUris,
            permissions: request.Permissions,
            clientType: request.Type,
            createdById: _currentUser.UserId
        );

        await _applicationRepository.AddAsync(app, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateApplicationResult(app.Id, request.Type == "confidential" ? rawSecret : null);
    }
}

