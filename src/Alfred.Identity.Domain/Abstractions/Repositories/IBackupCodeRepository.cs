using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IBackupCodeRepository : IRepository<BackupCode>
{
    Task<List<BackupCode>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BackupCode?> GetByCodeHashAsync(Guid userId, string codeHash, CancellationToken cancellationToken = default);
}
