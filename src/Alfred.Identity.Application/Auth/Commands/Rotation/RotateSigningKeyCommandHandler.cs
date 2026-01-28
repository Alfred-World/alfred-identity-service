using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Rotation;

public class RotateSigningKeyCommandHandler : IRequestHandler<RotateSigningKeyCommand, RotateSigningKeyResult>
{
    private readonly ISigningKeyRepository _keyRepository;
    private readonly IJwksService _jwksService;

    private readonly IUnitOfWork _unitOfWork; // Or use ISigningKeyRepository.SaveChangesAsync if available. 
    // Wait, repositories in this project seem to have SaveChangesAsync directly. 
    // And ServiceCollectionExtensions adds UnitOfWork. 
    // Typically use repository to save.
    // ISigningKeyRepository (Step 160) implements IRepository. IRepository usually has SaveChangesAsync?
    // Let's check IRepository (Step 124 for UserRepo impl suggests IRepository has it).
    // Ah, Step 160 ISigningKeyRepository inherits IRepository<SigningKey>.
    // Step 123 IUserRepository also inherits it and ADDS SaveChangesAsync.
    // Does IRepository HAVE SaveChangesAsync?
    // Step 105 (PostgresDbContext) doesn't show interface.
    // Step 50 lists Abstractions/Repositories/Interfaces... I saw IRepository? No I didn't verify IRepository content.
    // But JwksService (Step 164) uses _unitOfWork.SaveChangesAsync.
    // I should probably use _keyRepository.AddAsync and then _unitOfWork.SaveChangesAsync or _keyRepository.SaveChangesAsync.
    // UserRepo had explicit SaveChangesAsync.
    // ISigningKeyRepository (step 160) does NOT show SaveChangesAsync explicit.
    // So I should use IUnitOfWork or cast.
    // JwksService uses IUnitOfWork. I'll use that too if injected, OR assume IRepository has it.
    // Ideally use IUnitOfWork for transactional consistency.

    public RotateSigningKeyCommandHandler(ISigningKeyRepository keyRepository, IJwksService jwksService,
        IUnitOfWork unitOfWork)
    {
        _keyRepository = keyRepository;
        _jwksService = jwksService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RotateSigningKeyResult> Handle(RotateSigningKeyCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get current active key
        var currentKey = await _keyRepository.GetActiveKeyAsync(cancellationToken);

        // 2. Generate new key
        var newKey = _jwksService.GenerateSigningKey();

        // 3. Deactivate old key if exists
        if (currentKey != null)
        {
            currentKey.Deactivate();
            // _keyRepository.Update(currentKey); // Explicit update might be needed
            // Repository usually tracks changes.
        }

        // 4. Add new key
        await _keyRepository.AddAsync(newKey, cancellationToken);

        // 5. Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RotateSigningKeyResult(true, newKey.KeyId);
    }
}
