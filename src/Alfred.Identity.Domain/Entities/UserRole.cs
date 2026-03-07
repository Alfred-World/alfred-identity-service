using Alfred.Identity.Domain.Common.Interfaces;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a user-role mapping, aligned with AspNetUserRoles schema
/// Note: This is an association table entity, might not inherit BaseEntity if composite key is used directly
/// </summary>
public sealed class UserRole : IHasCreationTime, IHasCreator
{
    public UserId UserId { get; private set; }
    public RoleId RoleId { get; private set; }

    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }

    private UserRole()
    {
    }

    public static UserRole Create(UserId userId, RoleId roleId, Guid? createdById = null)
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
