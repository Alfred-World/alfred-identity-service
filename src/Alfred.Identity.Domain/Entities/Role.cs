using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Common.Interfaces;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a role, aligned with AspNetRoles schema.
/// Protection is based on Owner ID hardcode + IsImmutable flag.
/// Owner role is marked as IsImmutable=true and cannot be modified/assigned.
/// </summary>
public class Role : BaseEntity, IHasCreationTime, IHasCreator, IHasModificationTime, IHasModifier, IHasDeletionTime,
    IHasDeleter
{
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
    public string? Icon { get; private set; }
    public string? ConcurrencyStamp { get; private set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// If true, this role cannot be deleted or assigned to other users.
    /// Used for Owner role - the "Immutable God" role.
    /// </summary>
    public bool IsImmutable { get; private set; }

    /// <summary>
    /// If true, this is a system role that cannot be deleted (but can be assigned).
    /// Used for Admin, User roles.
    /// </summary>
    public bool IsSystem { get; private set; }

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public long? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedById { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public long? DeletedById { get; set; }

    /// <summary>
    /// Navigation property for Role-Permission relationship
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    public void Update(string name, string? icon)
    {
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Icon = icon;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPermission(long permissionId, long? creatorId = null)
    {
        if (RolePermissions.Any(rp => rp.PermissionId == permissionId))
        {
            return;
        }

        RolePermissions.Add(RolePermission.Create(Id, permissionId, creatorId));
    }

    public void RemovePermission(long permissionId)
    {
        var permission = RolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (permission != null)
        {
            RolePermissions.Remove(permission);
        }
    }

    public void SyncPermissions(IEnumerable<long> permissionIds, long? creatorId = null)
    {
        var desiredIds = new HashSet<long>(permissionIds);
        var currentIds = new HashSet<long>(RolePermissions.Select(rp => rp.PermissionId));

        // Identify permissions to add
        var toAdd = desiredIds.Except(currentIds);
        foreach (var id in toAdd)
        {
            AddPermission(id, creatorId);
        }

        // Identify permissions to remove
        var toRemove = currentIds.Except(desiredIds);
        foreach (var id in toRemove)
        {
            RemovePermission(id);
        }
    }

    private Role()
    {
    }

    public static Role Create(string name, string? icon = null, bool isImmutable = false, bool isSystem = false, long? createdById = null)
    {
        return new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Icon = icon,
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsImmutable = isImmutable,
            IsSystem = isSystem,
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById
        };
    }

    /// <summary>
    /// Creates the Owner role - highest authority, cannot be modified or assigned to others
    /// </summary>
    public static Role CreateOwner()
    {
        return Create("Owner", "tabler-shield-lock-filled", true, true);
    }

    /// <summary>
    /// Creates the Admin role - can manage users but not Owner
    /// </summary>
    public static Role CreateAdmin()
    {
        return Create("Admin", "tabler-shield-lock", false, true);
    }

    /// <summary>
    /// Creates the default User role
    /// </summary>
    public static Role CreateUser()
    {
        return Create("User", "tabler-user", false, true);
    }

    /// <summary>
    /// Returns true if this is the Owner role (untouchable)
    /// </summary>
    public bool IsOwnerRole()
    {
        return NormalizedName == "OWNER";
    }

    /// <summary>
    /// Returns true if this is the Admin role
    /// </summary>
    public bool IsAdminRole()
    {
        return NormalizedName == "ADMIN";
    }
}
