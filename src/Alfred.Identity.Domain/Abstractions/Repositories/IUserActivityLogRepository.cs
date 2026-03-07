using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IUserActivityLogRepository : IRepository<UserActivityLog, UserActivityLogId>
{
    Task<(List<UserActivityLog> Items, int TotalCount)> GetPagedAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
