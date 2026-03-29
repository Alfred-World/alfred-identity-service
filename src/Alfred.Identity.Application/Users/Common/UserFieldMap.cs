using System.Linq.Expressions;

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
        // Status is a UserStatus enum but UserDto.Status is string? —
        // calling .ToString() here makes the expression-tree type string,
        // so ProjectionBinder can bind it directly without a Convert.
        .Add("status", u => u.Status.ToString()).AllowAll()
        .Add("emailConfirmed", u => u.EmailConfirmed).AllowAll()
        .Add("phoneNumber", u => u.PhoneNumber).AllowAll()
        .Add("createdAt", u => u.CreatedAt).AllowAll()
        .Add("avatar", u => u.Avatar).AllowAll()

        // Full role projection — EF Core translates the Select as a correlated subquery;
        // no explicit Include() needed when using DB-level projection.
        .Add("roles", u => u.UserRoles.Select(ur => new RoleDto
        {
            Id = ur.Role.Id.Value,
            Name = ur.Role.Name,
            Icon = ur.Role.Icon
        })).AllowAll()

        // Lightweight role projection — only id and name (for summary views)
        .Add("rolesSummary", u => u.UserRoles.Select(ur => new RoleDto
        {
            Id = ur.Role.Id.Value,
            Name = ur.Role.Name
        })).Selectable();

    /// <summary>
    /// Available views for User entity.
    /// Each view defines which fields are projected at database level.
    /// </summary>
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
            u => u.Avatar
        })
        // Detail view with lightweight roles (only id, name)
        .Register("detail", cfg => cfg
            .Select(u => u.Id)
            .Select(u => u.UserName)
            .Select(u => u.Email)
            .Select(u => u.FullName)
            .Select(u => u.Status)
            .Select(u => u.EmailConfirmed)
            .Select(u => u.PhoneNumber)
            .Select(u => u.CreatedAt)
            .Select(u => u.Avatar)
            // Map DTO's Roles property to use rolesSummary from FieldMap
            .SelectAs(u => u.Roles, "rolesSummary"))
        // Detail view with full roles
        .Register("detail.full", new Expression<Func<UserDto, object?>>[]
        {
            u => u.Id,
            u => u.UserName,
            u => u.Email,
            u => u.FullName,
            u => u.Status,
            u => u.EmailConfirmed,
            u => u.PhoneNumber,
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
