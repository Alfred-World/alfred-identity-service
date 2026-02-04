using Alfred.Identity.Domain.Common.Base;

namespace Alfred.Identity.Domain.Entities;

public class UserActivityLog : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime OccurredAt { get; private set; }

    // Navigation
    public virtual User User { get; private set; } = null!;

    private UserActivityLog() { }

    public static UserActivityLog Create(
        Guid userId,
        string action,
        string? description = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new UserActivityLog
        {
            UserId = userId,
            Action = action,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            OccurredAt = DateTime.UtcNow
        };
    }
}
