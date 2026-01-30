using System.Linq.Expressions;

using Alfred.Identity.Application.Querying.Fields;

using ApplicationEntity = Alfred.Identity.Domain.Entities.Application;

namespace Alfred.Identity.Application.Applications.Common;

/// <summary>
/// Field map for Application domain entity - defines allowed fields for filtering and sorting
/// </summary>
public class ApplicationFieldMap : BaseFieldMap<ApplicationEntity>
{
    private static readonly Lazy<ApplicationFieldMap> _instance = new(() => new ApplicationFieldMap());

    private ApplicationFieldMap()
    {
    }

    public static ApplicationFieldMap Instance => _instance.Value;

    public override FieldMap<ApplicationEntity> Fields { get; } = new FieldMap<ApplicationEntity>()
        .Add("id", s => s.Id).AllowAll()
        .Add("clientId", s => s.ClientId).AllowAll()
        .Add("displayName", s => s.DisplayName!).AllowAll()
        .Add("applicationType", s => s.ApplicationType!).AllowAll()
        .Add("clientType", s => s.ClientType!).AllowAll()
        .Add("isActive", s => s.IsActive).AllowAll()
        .Add("createdAt", s => s.CreatedAt).AllowAll()
        .Add("updatedAt", s => s.UpdatedAt!).AllowAll();

    protected override Dictionary<string, Expression<Func<ApplicationEntity, object>>> AllowedIncludes { get; } =
        new(StringComparer.OrdinalIgnoreCase);
}
