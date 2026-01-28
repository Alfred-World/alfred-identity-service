using Alfred.Identity.Domain.Common.Interfaces;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a role-permission mapping (many-to-many relationship).
/// This is the join table between Role and Permission.
/// </summary>
public class RolePermission : IHasCreationTime, IHasCreator
{
    public long RoleId { get; private set; }
    public long PermissionId { get; private set; }

    /// <summary>
    /// Navigation property to the Role
    /// </summary>
    public virtual Role Role { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the Permission
    /// </summary>
    public virtual Permission Permission { get; private set; } = null!;

    public DateTime CreatedAt { get; set; }
    public long? CreatedById { get; set; }

    private RolePermission()
    {
    }

    /// <summary>
    /// Creates a new role-permission mapping
    /// </summary>
    public static RolePermission Create(long roleId, long permissionId, long? createdById = null)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById
        };
    }
}
