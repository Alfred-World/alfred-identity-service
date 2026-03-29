namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a role-permission mapping (many-to-many relationship).
/// This is the join table between Role and Permission.
/// </summary>
public sealed class RolePermission : IHasCreationTime, IHasCreator
{
    public RoleId RoleId { get; private set; }
    public PermissionId PermissionId { get; private set; }

    /// <summary>
    /// Navigation property to the Role
    /// </summary>
    public Role Role { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the Permission
    /// </summary>
    public Permission Permission { get; private set; } = null!;

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }

    private RolePermission()
    {
    }

    /// <summary>
    /// Creates a new role-permission mapping
    /// </summary>
    public static RolePermission Create(RoleId roleId, PermissionId permissionId, Guid? createdById = null)
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
