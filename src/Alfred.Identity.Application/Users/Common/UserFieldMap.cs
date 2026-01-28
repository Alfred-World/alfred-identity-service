using System.Linq.Expressions;

using Alfred.Identity.Application.Querying;
using Alfred.Identity.Application.Querying.Validation;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Users.Common;

/// <summary>
/// FieldMap for User entity - defines filterable and sortable fields
/// </summary>
public class UserFieldMap : BaseFieldMap<User>
{
    private static readonly Lazy<UserFieldMap> _instance = new(() => new UserFieldMap());

    private UserFieldMap()
    {
    }

    public static UserFieldMap Instance => _instance.Value;

    public override FieldMap<User> Fields { get; } = new FieldMap<User>()
        .Add("id", u => u.Id)
        .Add("userName", u => u.UserName)
        .Add("email", u => u.Email)
        .Add("fullName", u => u.FullName)
        .Add("status", u => u.Status)
        .Add("emailConfirmed", u => u.EmailConfirmed)
        .Add("createdAt", u => u.CreatedAt);

    protected override Dictionary<string, Expression<Func<User, object>>> AllowedIncludes { get; } = new()
    {
        // User navigations not exposed to clients for security
    };
}
