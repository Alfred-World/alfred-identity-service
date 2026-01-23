using Alfred.Identity.Domain.Common.Base;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a permission that can be assigned to roles.
/// Permissions follow the pattern: "resource:action" (e.g., "finance:read", "server:reboot")
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>
    /// Unique code for the permission (e.g., "finance:write")
    /// </summary>
    public string Code { get; private set; } = null!;

    /// <summary>
    /// Human-readable name (e.g., "Write Finance Data")
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Optional description of what this permission allows
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Resource group this permission belongs to (e.g., "finance", "server")
    /// Derived from the Code prefix for easier filtering
    /// </summary>
    public string Resource { get; private set; } = null!;

    /// <summary>
    /// Action type (e.g., "read", "write", "delete", "admin")
    /// Derived from the Code suffix
    /// </summary>
    public string Action { get; private set; } = null!;

    /// <summary>
    /// Whether this permission is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Navigation property for Role-Permission many-to-many relationship
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private Permission()
    {
    }

    /// <summary>
    /// Creates a new Permission with the given code and name.
    /// Resource and Action are automatically derived from the Code.
    /// </summary>
    /// <param name="code">Permission code in format "resource:action"</param>
    /// <param name="name">Human-readable name</param>
    /// <param name="description">Optional description</param>
    public static Permission Create(string code, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code, nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        var parts = code.Split(':');
        var resource = parts.Length > 0 ? parts[0] : code;
        var action = parts.Length > 1 ? parts[1] : "access";

        return new Permission
        {
            Code = code.ToLowerInvariant(),
            Name = name,
            Description = description,
            Resource = resource.ToLowerInvariant(),
            Action = action.ToLowerInvariant(),
            IsActive = true
        };
    }

    /// <summary>
    /// Updates the permission details
    /// </summary>
    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Deactivates this permission
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Reactivates this permission
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
