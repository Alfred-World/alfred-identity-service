using System.Linq.Expressions;

using Alfred.Identity.Application.Querying;
using Alfred.Identity.Application.Querying.Validation;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Permissions.Common;

/// <summary>
/// FieldMap for Permission entity - defines filterable and sortable fields
/// </summary>
public class PermissionFieldMap : BaseFieldMap<Permission>
{
    private static readonly Lazy<PermissionFieldMap> _instance = new(() => new PermissionFieldMap());

    private PermissionFieldMap()
    {
    }

    public static PermissionFieldMap Instance => _instance.Value;

    public override FieldMap<Permission> Fields { get; } = new FieldMap<Permission>()
        .Add("id", p => p.Id)
        .Add("code", p => p.Code)
        .Add("name", p => p.Name)
        .Add("description", p => p.Description!)
        .Add("resource", p => p.Resource)
        .Add("action", p => p.Action)
        .Add("isActive", p => p.IsActive)
        .Add("createdAt", p => p.CreatedAt);

    protected override Dictionary<string, Expression<Func<Permission, object>>> AllowedIncludes { get; } = new()
    {
        // Permission has no navigations for clients
    };
}
