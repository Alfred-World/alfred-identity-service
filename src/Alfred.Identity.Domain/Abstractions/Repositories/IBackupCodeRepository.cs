using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IBackupCodeRepository : IRepository<BackupCode, BackupCodeId>
{
    Task<List<BackupCode>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<BackupCode?> GetByCodeHashAsync(UserId userId, string codeHash, CancellationToken cancellationToken = default);
}
