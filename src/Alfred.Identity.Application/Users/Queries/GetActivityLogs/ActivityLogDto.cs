using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Users.Queries.GetActivityLogs;

public record ActivityLogDto(
    Guid UserId,
    string Action,
    string? Description,
    string? IpAddress,
    string? UserAgent,
    DateTime OccurredAt)
{
    public static ActivityLogDto FromEntity(UserActivityLog log)
    {
        return new ActivityLogDto(
            log.UserId,
            log.Action,
            log.Description,
            log.IpAddress,
            log.UserAgent,
            log.OccurredAt);
    }
}
