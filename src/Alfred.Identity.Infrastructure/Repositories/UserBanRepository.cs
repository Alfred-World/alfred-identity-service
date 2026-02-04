using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Identity;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;


namespace Alfred.Identity.Infrastructure.Repositories;

public class UserBanRepository : BaseRepository<UserBan>, IUserBanRepository
{
    private readonly IDbContext _context;

    public UserBanRepository(IDbContext context) : base(context)

    {
        _context = context;
    }

    public async Task<List<UserBan>> GetHistoryByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserBan>()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BannedAt)
            .ToListAsync(cancellationToken);
    }
}
