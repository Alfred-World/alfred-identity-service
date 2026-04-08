using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Application.Roles.Common;

/// <summary>
/// Typed filter input for Role entity — enables Swagger/OpenAPI to describe
/// available fields and operators so frontend codegen produces autocomplete-friendly types.
/// <para>
/// Usage: <c>{ "name": { "contains": "admin" }, "isSystem": { "eq": true } }</c>
/// </para>
/// </summary>
public sealed class RoleFilterInput : FilterInputBase<RoleFilterInput>
{
    public GuidFilterInput? Id { get; set; }
    public StringFilterInput? Name { get; set; }
    public StringFilterInput? NormalizedName { get; set; }
    public BoolFilterInput? IsImmutable { get; set; }
    public BoolFilterInput? IsSystem { get; set; }
    public StringFilterInput? Icon { get; set; }
    public BoolFilterInput? IsDeleted { get; set; }
    public DateTimeFilterInput? CreatedAt { get; set; }
    public DateTimeFilterInput? UpdatedAt { get; set; }
}
