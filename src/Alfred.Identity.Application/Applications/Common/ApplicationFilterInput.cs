using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Application.Applications.Common;

/// <summary>
/// Typed filter input for Application entity — enables Swagger/OpenAPI to describe
/// available fields and operators so frontend codegen produces autocomplete-friendly types.
/// <para>
/// Usage: <c>{ "clientId": { "eq": "my-app" }, "isActive": { "eq": true } }</c>
/// </para>
/// </summary>
public sealed class ApplicationFilterInput : FilterInputBase<ApplicationFilterInput>
{
    public GuidFilterInput? Id { get; set; }
    public StringFilterInput? ClientId { get; set; }
    public StringFilterInput? DisplayName { get; set; }
    public StringFilterInput? ApplicationType { get; set; }
    public StringFilterInput? ClientType { get; set; }
    public BoolFilterInput? IsActive { get; set; }
    public DateTimeFilterInput? CreatedAt { get; set; }
    public DateTimeFilterInput? UpdatedAt { get; set; }
}
