using System.Linq.Expressions;

using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Projection;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Roles.Common;

/// <summary>
/// FieldMap for Role entity - defines filterable, sortable fields and views
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
        .Add("createdAt", r => r.CreatedAt).AllowAll()
        .Add("updatedAt", r => r.UpdatedAt!).AllowAll()
        .Add("permissions", r => r.RolePermissions.Select(rp => new PermissionDto(
            rp.Permission.Id,
            rp.Permission.Code,
            rp.Permission.Name,
            rp.Permission.Description,
            rp.Permission.Resource,
            rp.Permission.Action,
            rp.Permission.IsActive
        ))).AllowAll();

    /// <summary>
    /// Available views for Role entity
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
        .Register("detail", new Expression<Func<RoleDto, object?>>[]
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

            // Permissions
            r => r.Permissions
        })
        .Register("summary", new Expression<Func<RoleDto, object?>>[]
        {
            r => r.Id,
            r => r.Name,
            r => r.Icon
        })
        .SetDefault("list");

    protected override Dictionary<string, Expression<Func<Role, object>>> AllowedIncludes { get; } = new()
    {
        // Role entity has no navigations that need to be included
    };
}
