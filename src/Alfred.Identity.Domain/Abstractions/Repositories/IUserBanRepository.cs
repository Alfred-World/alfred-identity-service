using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IUserBanRepository : IRepository<UserBan, UserBanId>
{
    Task<List<UserBan>> GetHistoryByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
}
