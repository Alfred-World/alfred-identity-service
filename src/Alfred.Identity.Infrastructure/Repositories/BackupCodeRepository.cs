using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

public class BackupCodeRepository : BaseRepository<BackupCode>, IBackupCodeRepository
{
    public BackupCodeRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<List<BackupCode>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
    }

    public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var codes = await DbSet.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        DbSet.RemoveRange(codes);
    }

    public async Task<BackupCode?> GetByCodeHashAsync(Guid userId, string codeHash, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.UserId == userId && x.CodeHash == codeHash, cancellationToken);
    }
}
