using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

public interface IUserBanRepository : IRepository<UserBan>
{
    Task<List<UserBan>> GetHistoryByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
