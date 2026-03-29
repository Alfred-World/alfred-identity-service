using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.TwoFactor;

/// <summary>
/// Deletes all existing backup codes for the user and generates 10 new single-use ones.
/// </summary>
public class RegenerateBackupCodesCommandHandler
    : IRequestHandler<RegenerateBackupCodesCommand, Result<IEnumerable<string>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IBackupCodeRepository _backupCodeRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegenerateBackupCodesCommandHandler(
        IUserRepository userRepository,
        ITwoFactorService twoFactorService,
        IBackupCodeRepository backupCodeRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
        _backupCodeRepository = backupCodeRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<IEnumerable<string>>> Handle(
        RegenerateBackupCodesCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<IEnumerable<string>>.Failure("User not found");
        }

        if (!user.TwoFactorEnabled)
        {
            return Result<IEnumerable<string>>.Failure("Two-factor authentication is not enabled.");
        }

        // Invalidate all existing codes
        await _backupCodeRepository.DeleteByUserIdAsync(user.Id, cancellationToken);

        // Generate 10 fresh single-use codes
        var plainCodes = _twoFactorService.GenerateBackupCodes(10);
        var entities = new List<BackupCode>(plainCodes.Length);

        foreach (var code in plainCodes)
        {
            var hash = _passwordHasher.HashPassword(code);
            entities.Add(BackupCode.Create(hash, user.Id));
        }

        foreach (var entity in entities)
        {
            await _backupCodeRepository.AddAsync(entity, cancellationToken);
        }

        await _backupCodeRepository.SaveChangesAsync(cancellationToken);

        return Result<IEnumerable<string>>.Success(plainCodes);
    }
}
