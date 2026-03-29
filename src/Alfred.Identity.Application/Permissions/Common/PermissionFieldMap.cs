using System.Linq.Expressions;

using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Projection;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Permissions.Common;

/// <summary>
/// FieldMap for Permission entity - defines filterable, sortable fields.
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

    public static ViewRegistry<Permission, PermissionDto> Views { get; } =
        new ViewRegistry<Permission, PermissionDto>()
            .Register("list", new Expression<Func<PermissionDto, object?>>[]
            {
                x => x.Id,
                x => x.Code,
                x => x.Name,
                x => x.Description,
                x => x.Resource,
                x => x.Action,
                x => x.IsActive
            })
            .SetDefault("list");
}
