using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Users.Common;

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
            log.UserId.Value,
            log.Action,
            log.Description,
            log.IpAddress,
            log.UserAgent,
            log.OccurredAt);
    }
}

public record ActivityLogPageResult(List<ActivityLogDto> Items, int TotalCount, int Page, int PageSize);
