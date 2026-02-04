using System.Linq.Expressions;

using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Projection;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Roles.Common;

/// <summary>
/// FieldMap for Role entity - defines filterable, sortable fields and views.
/// Optimized for database-level projection to minimize memory usage.
/// </summary>
public class RoleFieldMap : BaseFieldMap<Role>
{
    private static readonly Lazy<RoleFieldMap> _instance = new(() => new RoleFieldMap());

    private RoleFieldMap()
    {
    }

    public static RoleFieldMap Instance => _instance.Value;

    public override FieldMap<Role> Fields { get; } = new FieldMap<Role>()
        .Add("id", r => r.Id).AllowAll()
        .Add("name", r => r.Name).AllowAll()
        .Add("normalizedName", r => r.NormalizedName).AllowAll()
        .Add("isImmutable", r => r.IsImmutable).AllowAll()
        .Add("isSystem", r => r.IsSystem).AllowAll()
        .Add("icon", r => r.Icon!).AllowAll()
        .Add("isDeleted", r => r.IsDeleted).AllowAll()
        .Add("createdAt", r => r.CreatedAt).AllowAll()
        .Add("updatedAt", r => r.UpdatedAt!).AllowAll()

        // Full permission projection - all fields
        .Add("permissions", r => r.RolePermissions.Select(rp => new PermissionDto(
            rp.Permission.Id,
            rp.Permission.Code,
            rp.Permission.Name,
            rp.Permission.Description,
            rp.Permission.Resource,
            rp.Permission.Action,
            rp.Permission.IsActive
        ))).AllowAll()

        // Lightweight permission projection - only id, code, name (for list views)
        // Returns PermissionDto with null for non-essential fields (skipped in JSON)
        .Add("permissionsSummary", r => r.RolePermissions.Select(rp => new PermissionDto(
            rp.Permission.Id,
            rp.Permission.Code,
            rp.Permission.Name,
            null,  // Description - skipped in JSON
            null,  // Resource - skipped in JSON  
            null,  // Action - skipped in JSON
            null   // IsActive - skipped in JSON
        ))).Selectable();

    /// <summary>
    /// Available views for Role entity.
    /// Each view defines which fields are projected at database level.
    /// </summary>
    public static ViewRegistry<Role, RoleDto> Views { get; } = new ViewRegistry<Role, RoleDto>()
        .Register("list", new Expression<Func<RoleDto, object?>>[]
        {
            r => r.Id,
            r => r.Name,
            r => r.NormalizedName,
            r => r.IsImmutable,
            r => r.IsSystem,
            r => r.Icon,
            r => r.IsDeleted,
            r => r.CreatedAt,
            r => r.UpdatedAt
        })
        // Detail view with lightweight permissions (only id, code, name)
        .Register("detail", cfg => cfg
            .Select(r => r.Id)
            .Select(r => r.Name)
            .Select(r => r.NormalizedName)
            .Select(r => r.IsImmutable)
            .Select(r => r.IsSystem)
            .Select(r => r.Icon)
            .Select(r => r.IsDeleted)
            .Select(r => r.CreatedAt)
            .Select(r => r.UpdatedAt)
            // Map DTO's Permissions property to use permissionsSummary from FieldMap
            .SelectAs(r => r.Permissions, "permissionsSummary"))
        // Detail view with full permissions (all fields)
        .Register("detail.full", new Expression<Func<RoleDto, object?>>[]
        {
            r => r.Id,
            r => r.Name,
            r => r.NormalizedName,
            r => r.IsImmutable,
            r => r.IsSystem,
            r => r.Icon,
            r => r.IsDeleted,
            r => r.CreatedAt,
            r => r.UpdatedAt,
            r => r.Permissions
        })
        .Register("summary", new Expression<Func<RoleDto, object?>>[]
        {
            r => r.Id,
            r => r.Name,
            r => r.Icon
        })
        .SetDefault("list");
}
