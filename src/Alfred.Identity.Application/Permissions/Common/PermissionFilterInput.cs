using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Application.Permissions.Common;

/// <summary>
/// Typed filter input for Permission entity — enables Swagger/OpenAPI to describe
/// available fields and operators so frontend codegen produces autocomplete-friendly types.
/// <para>
/// Usage: <c>{ "resource": { "eq": "users" }, "action": { "eq": "read" } }</c>
/// </para>
/// </summary>
public sealed class PermissionFilterInput : FilterInputBase<PermissionFilterInput>
{
    public GuidFilterInput? Id { get; set; }
    public StringFilterInput? Code { get; set; }
    public StringFilterInput? Name { get; set; }
    public StringFilterInput? Description { get; set; }
    public StringFilterInput? Resource { get; set; }
    public StringFilterInput? Action { get; set; }
    public BoolFilterInput? IsActive { get; set; }
    public DateTimeFilterInput? CreatedAt { get; set; }
}
