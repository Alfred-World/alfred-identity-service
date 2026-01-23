using Alfred.Identity.Domain.Common.Base;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a role, aligned with AspNetRoles schema.
/// Protection is based on Owner ID hardcode + IsImmutable flag.
/// Owner role is marked as IsImmutable=true and cannot be modified/assigned.
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string NormalizedName { get; private set; } = null!;
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

    /// <summary>
    /// Navigation property for Role-Permission relationship
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private Role()
    {
    }

    public static Role Create(string name, bool isImmutable = false, bool isSystem = false)
    {
        return new Role
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsImmutable = isImmutable,
            IsSystem = isSystem
        };
    }

    /// <summary>
    /// Creates the Owner role - highest authority, cannot be modified or assigned to others
    /// </summary>
    public static Role CreateOwner()
    {
        return Create("Owner", isImmutable: true, isSystem: true);
    }

    /// <summary>
    /// Creates the Admin role - can manage users but not Owner
    /// </summary>
    public static Role CreateAdmin()
    {
        return Create("Admin", isImmutable: false, isSystem: true);
    }

    /// <summary>
    /// Creates the default User role
    /// </summary>
    public static Role CreateUser()
    {
        return Create("User", isImmutable: false, isSystem: true);
    }

    /// <summary>
    /// Returns true if this is the Owner role (untouchable)
    /// </summary>
    public bool IsOwnerRole() => NormalizedName == "OWNER";

    /// <summary>
    /// Returns true if this is the Admin role
    /// </summary>
    public bool IsAdminRole() => NormalizedName == "ADMIN";
}


