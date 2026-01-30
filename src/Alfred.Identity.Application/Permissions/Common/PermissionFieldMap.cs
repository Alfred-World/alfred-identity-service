using System.Linq.Expressions;

using Alfred.Identity.Application.Querying.Fields;
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
        .Add("id", p => p.Id).AllowAll()
        .Add("code", p => p.Code).AllowAll()
        .Add("name", p => p.Name).AllowAll()
        .Add("description", p => p.Description!).AllowAll()
        .Add("resource", p => p.Resource).AllowAll()
        .Add("action", p => p.Action).AllowAll()
        .Add("isActive", p => p.IsActive).AllowAll()
        .Add("createdAt", p => p.CreatedAt).AllowAll();

    protected override Dictionary<string, Expression<Func<Permission, object>>> AllowedIncludes { get; } = new()
    {
        // Permission has no navigations for clients
    };
}
