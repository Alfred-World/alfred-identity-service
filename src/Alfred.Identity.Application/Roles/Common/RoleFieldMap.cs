using System.Linq.Expressions;

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
        .Add("createdAt", r => r.CreatedAt).AllowAll()
        .Add("updatedAt", r => r.UpdatedAt!).AllowAll();

    /// <summary>
    /// Available views for Role entity
    /// </summary>
    public static ViewRegistry<Role, RoleDto> Views { get; } = new ViewRegistry<Role, RoleDto>()
        .Register("list", new Expression<Func<RoleDto, object?>>[]
        {
            r => r.Id, r => r.Name, r => r.IsImmutable, r => r.IsSystem, r => r.Icon
        })
        .Register("detail", new Expression<Func<RoleDto, object?>>[]
        {
            r => r.Id, r => r.Name, r => r.NormalizedName, r => r.IsImmutable, r => r.IsSystem, r => r.Icon,
            r => r.CreatedAt, r => r.UpdatedAt
        })
        .Register("minimal", new Expression<Func<RoleDto, object?>>[]
        {
            r => r.Id, r => r.Name
        })
        .SetDefault("list");

    protected override Dictionary<string, Expression<Func<Role, object>>> AllowedIncludes { get; } = new()
    {
        // Role entity has no navigations that need to be included
    };
}
