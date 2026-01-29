using System.Linq.Expressions;

using Alfred.Identity.Application.Querying;
using Alfred.Identity.Application.Querying.Validation;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Roles.Common;

/// <summary>
/// FieldMap for Role entity - defines filterable and sortable fields
/// </summary>
public class RoleFieldMap : BaseFieldMap<Role>
{
    private static readonly Lazy<RoleFieldMap> _instance = new(() => new RoleFieldMap());

    private RoleFieldMap()
    {
    }

    public static RoleFieldMap Instance => _instance.Value;

    public override FieldMap<Role> Fields { get; } = new FieldMap<Role>()
        .Add("id", r => r.Id)
        .Add("name", r => r.Name)
        .Add("normalizedName", r => r.NormalizedName)
        .Add("isImmutable", r => r.IsImmutable)
        .Add("isSystem", r => r.IsSystem)
        .Add("createdAt", r => r.CreatedAt)
        .Add("updatedAt", r => r.UpdatedAt!);

    protected override Dictionary<string, Expression<Func<Role, object>>> AllowedIncludes { get; } = new()
    {
        // Role entity has no navigations that need to be included 
    };
}
