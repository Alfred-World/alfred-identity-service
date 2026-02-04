using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;
using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.TwoFactor;

public class ConfirmEnableTwoFactorCommandHandler : IRequestHandler<ConfirmEnableTwoFactorCommand, Result<IEnumerable<string>>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IBackupCodeRepository _backupCodeRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ConfirmEnableTwoFactorCommandHandler(
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

    public async Task<Result<IEnumerable<string>>> Handle(ConfirmEnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) return Result<IEnumerable<string>>.Failure("User not found");

        if (user.TwoFactorEnabled) return Result<IEnumerable<string>>.Failure("2FA already enabled");
        if (string.IsNullOrEmpty(user.TwoFactorSecret)) return Result<IEnumerable<string>>.Failure("Two-factor initiation required.");

        if (!_twoFactorService.ValidateCode(user.TwoFactorSecret, request.Code))
        {
            return Result<IEnumerable<string>>.Failure("Invalid code.");
        }

        // Enable 2FA
        user.EnableTwoFactor();
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Generate Backup Codes
        var plainCodes = _twoFactorService.GenerateBackupCodes();
        var backupCodeEntities = new List<BackupCode>();

        foreach (var code in plainCodes)
        {
            var hash = _passwordHasher.HashPassword(code);
            backupCodeEntities.Add(BackupCode.Create(hash, user.Id));
        }

        // Clear any existing codes (though unlikely if just enabling, but safe to do)
        await _backupCodeRepository.DeleteByUserIdAsync(user.Id, cancellationToken);
        
        foreach (var entity in backupCodeEntities)
        {
            await _backupCodeRepository.AddAsync(entity, cancellationToken);
        }
        await _backupCodeRepository.SaveChangesAsync(cancellationToken);

        return Result<IEnumerable<string>>.Success(plainCodes);
    }
}
