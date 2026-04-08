using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Application.Users.Common;

/// <summary>
/// Typed filter input for User entity — enables Swagger/OpenAPI to describe
/// available fields and operators so frontend codegen produces autocomplete-friendly types.
/// <para>
/// Usage: <c>{ "email": { "contains": "admin" }, "status": { "eq": "Active" } }</c>
/// Collection: <c>{ "roles": { "some": { "name": { "contains": "Admin" } } } }</c>
/// </para>
/// </summary>
public sealed class UserFilterInput : FilterInputBase<UserFilterInput>
{
    public GuidFilterInput? Id { get; set; }
    public StringFilterInput? UserName { get; set; }
    public StringFilterInput? Email { get; set; }
    public StringFilterInput? FullName { get; set; }
    public StringFilterInput? Status { get; set; }
    public BoolFilterInput? EmailConfirmed { get; set; }
    public StringFilterInput? PhoneNumber { get; set; }
    public DateTimeFilterInput? CreatedAt { get; set; }
    public StringFilterInput? Avatar { get; set; }

    /// <summary>
    /// Filter users by their roles (collection filter).
    /// Example: <c>{ "roles": { "some": { "name": { "eq": "Admin" } } } }</c>
    /// </summary>
    public CollectionFilterInput<RoleFilterInput>? Roles { get; set; }
}
