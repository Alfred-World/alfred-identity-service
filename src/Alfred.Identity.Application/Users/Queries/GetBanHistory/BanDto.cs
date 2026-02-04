using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Users.Queries.GetBanHistory;

public record BanDto(
    Guid UserId,
    string Reason,
    DateTime BannedAt,
    Guid? BannedById,
    DateTime? ExpiresAt,
    bool IsActive,
    DateTime? UnbannedAt,
    Guid? UnbannedById)
{
    public static BanDto FromEntity(UserBan ban)
    {
        return new BanDto(
            ban.UserId,
            ban.Reason,
            ban.BannedAt,
            ban.BannedById,
            ban.ExpiresAt,
            ban.IsActive,
            ban.UnbannedAt,
            ban.UnbannedById);
    }
}
