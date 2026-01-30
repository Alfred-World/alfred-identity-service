using System.Linq.Expressions;

using Alfred.Identity.Application.Querying.Fields;
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
        .Add("id", u => u.Id).AllowAll()
        .Add("userName", u => u.UserName).AllowAll()
        .Add("email", u => u.Email).AllowAll()
        .Add("fullName", u => u.FullName).AllowAll()
        .Add("status", u => u.Status).AllowAll()
        .Add("emailConfirmed", u => u.EmailConfirmed).AllowAll()
        .Add("createdAt", u => u.CreatedAt).AllowAll();

    protected override Dictionary<string, Expression<Func<User, object>>> AllowedIncludes { get; } = new()
    {
        // User navigations not exposed to clients for security
    };
}
