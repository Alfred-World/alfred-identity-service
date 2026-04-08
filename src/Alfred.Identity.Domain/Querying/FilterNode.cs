using System.Text.Json.Serialization;

namespace Alfred.Identity.Domain.Querying;

/// <summary>
/// Base class for JSON filter DSL nodes (HotChocolate-inspired).
/// Supports logical combinators, field-level comparisons, and collection traversal.
/// </summary>
[JsonConverter(typeof(FilterNodeJsonConverter))]
public abstract record FilterNode;

/// <summary>
/// Logical AND/OR combining multiple filter conditions.
/// JSON: { "and": [...] } or { "or": [...] }
/// </summary>
public sealed record LogicalFilterNode(LogicalOperator Operator, IReadOnlyList<FilterNode> Conditions) : FilterNode;

/// <summary>
/// Field-level filter with one or more comparison operations.
/// JSON: { "email": { "contains": "admin", "neq": "test" } }
/// Multiple operations on the same field are implicitly AND-ed.
/// </summary>
public sealed record FieldFilterNode(string FieldName, IReadOnlyList<FieldOperation> Operations) : FilterNode;

/// <summary>
/// Collection/navigation property filter.
/// JSON: { "roles": { "some": { "name": { "eq": "Admin" } } } }
/// </summary>
public sealed record CollectionFilterNode(
    string FieldName,
    CollectionOperator Operator,
    FilterNode? InnerFilter) : FilterNode;

/// <summary>
/// Single comparison operation on a field.
/// The Value can be: string, long, double, bool, null, or List&lt;object?&gt; (for in/nin).
/// </summary>
public sealed record FieldOperation(string Operator, object? Value);

public enum LogicalOperator
{
    And,
    Or
}

public enum CollectionOperator
{
    Some,
    All,
    None,
    Any
}
