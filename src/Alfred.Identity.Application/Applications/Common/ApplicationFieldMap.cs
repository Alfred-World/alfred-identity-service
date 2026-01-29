using System.Linq.Expressions;

using Alfred.Identity.Application.Querying;
using Alfred.Identity.Application.Querying.Validation;

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
        .Add("id", s => s.Id)
        .Add("clientId", s => s.ClientId)
        .Add("displayName", s => s.DisplayName!)
        .Add("applicationType", s => s.ApplicationType!)
        .Add("clientType", s => s.ClientType!)
        .Add("isActive", s => s.IsActive)
        .Add("createdAt", s => s.CreatedAt)
        .Add("updatedAt", s => s.UpdatedAt!);

    protected override Dictionary<string, Expression<Func<ApplicationEntity, object>>> AllowedIncludes { get; } =
        new(StringComparer.OrdinalIgnoreCase);
}
