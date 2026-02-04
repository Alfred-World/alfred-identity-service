using System.Linq.Expressions;

using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Projection;
using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Application.Users.Common;

/// <summary>
/// FieldMap for User entity - defines filterable, sortable fields and views.
/// Optimized for database-level projection to minimize memory usage.
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
        .Add("createdAt", u => u.CreatedAt).AllowAll()
        .Add("avatar", u => u.Avatar).AllowAll()
        .Add("roles", u => u.UserRoles.Select(ur => new RoleDto
        {
            Id = ur.Role.Id,
            Name = ur.Role.Name,
            Icon = ur.Role.Icon
        })).AllowAll();

    public static ViewRegistry<User, UserDto> Views { get; } = new ViewRegistry<User, UserDto>()
        .Register("list", new Expression<Func<UserDto, object?>>[]
        {
            u => u.Id,
            u => u.UserName,
            u => u.Email,
            u => u.FullName,
            u => u.Status,
            u => u.EmailConfirmed,
            u => u.CreatedAt,
            u => u.Avatar,
            u => u.Roles
        })
        .Register("summary", new Expression<Func<UserDto, object?>>[]
        {
            u => u.Id,
            u => u.UserName,
            u => u.Email,
            u => u.FullName,
            u => u.PhoneNumber,
            u => u.Avatar
        })
        .SetDefault("list");
}
