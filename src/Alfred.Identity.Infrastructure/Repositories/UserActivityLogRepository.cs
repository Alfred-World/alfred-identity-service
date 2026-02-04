using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Common.Identity;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;


namespace Alfred.Identity.Infrastructure.Repositories;

public class UserActivityLogRepository : BaseRepository<UserActivityLog>, IUserActivityLogRepository
{
    private readonly IDbContext _context;

    public UserActivityLogRepository(IDbContext context) : base(context)

    {
        _context = context;
    }

    public async Task<(List<UserActivityLog> Items, int TotalCount)> GetPagedAsync(
        Guid userId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserActivityLog>()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.OccurredAt);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
