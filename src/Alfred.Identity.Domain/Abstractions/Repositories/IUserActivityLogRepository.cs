using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IUserActivityLogRepository : IRepository<UserActivityLog>
{
    Task<(List<UserActivityLog> Items, int TotalCount)> GetPagedAsync(
        Guid userId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default);
}
