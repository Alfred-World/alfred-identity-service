using System.Security.Cryptography;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;

using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.RegenerateSecret;

public class RegenerateClientSecretCommandHandler : IRequestHandler<RegenerateClientSecretCommand, string?>
{
    private readonly IApplicationRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegenerateClientSecretCommandHandler(
        IApplicationRepository repository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<string?> Handle(RegenerateClientSecretCommand request, CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (application == null)
        {
            return null; // Or throw NotFoundException
        }

        // Generate a secure random secret (32 bytes -> base64)
        var secretBytes = RandomNumberGenerator.GetBytes(32);
        var rawSecret = Convert.ToBase64String(secretBytes);

        var secretHash = _passwordHasher.HashPassword(rawSecret);

        application.RotateSecret(secretHash);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return rawSecret;
    }
}
