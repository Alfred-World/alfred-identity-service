using Alfred.Identity.Domain.Common.Interfaces;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a user-role mapping, aligned with AspNetUserRoles schema
/// Note: This is an association table entity, might not inherit BaseEntity if composite key is used directly
/// </summary>
public class UserRole : IHasCreationTime, IHasCreator
{
    public long UserId { get; private set; }
    public long RoleId { get; private set; }

    public virtual User User { get; private set; } = null!;
    public virtual Role Role { get; private set; } = null!;

    public DateTime CreatedAt { get; set; }
    public long? CreatedById { get; set; }

    private UserRole()
    {
    }

    public static UserRole Create(long userId, long roleId, long? createdById = null)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById
        };
    }
}
