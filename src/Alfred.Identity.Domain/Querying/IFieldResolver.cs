using System.Linq.Expressions;

namespace Alfred.Identity.Domain.Querying;

/// <summary>
/// Provides field metadata and expression resolution for an entity type.
/// Defined in Domain so Infrastructure (filter/sort binders) can use it
/// without referencing Application (where FieldMap lives).
/// </summary>
public interface IFieldResolver<T> where T : class
{
    /// <summary>
    /// Resolve a field name to its lambda expression and property type.
    /// Returns false if the field does not exist in the map.
    /// </summary>
    bool TryResolve(string fieldName, out LambdaExpression expression, out Type propertyType);

    /// <summary>
    /// Resolve the filter-specific expression for a field.
    /// For collection fields, this is typically a raw navigation expression
    /// (e.g. <c>u => u.UserRoles.Select(ur => ur.Role)</c>) that EF Core can
    /// translate in subquery predicates without going through DTO projections.
    /// Falls back to the regular expression when no dedicated filter expression is registered.
    /// </summary>
    bool TryResolveForFilter(string fieldName, out LambdaExpression expression, out Type propertyType)
    {
        return TryResolve(fieldName, out expression, out propertyType);
    }

    /// <summary>Whether the field can be used in filter conditions.</summary>
    bool CanFilter(string fieldName);

    /// <summary>Whether the field can be used in sort expressions.</summary>
    bool CanSort(string fieldName);

    /// <summary>Whether a field with this name exists in the resolver.</summary>
    bool ContainsField(string fieldName);

    /// <summary>
    /// For collection fields: returns the set of inner field names allowed in collection predicates
    /// (some/all/none/any). Returns null if no restriction is declared (all scalar fields then blocked
    /// by the binder's type check).
    /// </summary>
    IReadOnlySet<string>? GetAllowedInnerFields(string fieldName);

    /// <summary>
    /// Get metadata for all registered fields.
    /// Used by the operator metadata endpoint to inform the frontend.
    /// </summary>
    IEnumerable<FieldMeta> GetAllFieldMeta();
}

/// <summary>
/// Field metadata for frontend consumption.
/// </summary>
public sealed record FieldMeta(
    string Name,
    string TypeName,
    IReadOnlyList<string> Operators,
    bool CanFilter,
    bool CanSort,
    bool CanSelect);

/// <summary>
/// Search metadata response returned by the operator metadata endpoint.
/// </summary>
public sealed record SearchMetadataResponse
{
    /// <summary>Operators available per data type (string → [eq, neq, contains, ...]).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> TypeOperators { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Logical operators: ["and", "or"].</summary>
    public IReadOnlyList<string> LogicalOperators { get; init; } = ["and", "or"];

    /// <summary>Sort directions: ["asc", "desc"].</summary>
    public IReadOnlyList<string> SortDirections { get; init; } = ["asc", "desc"];

    /// <summary>Available fields with their types and operators.</summary>
    public IReadOnlyList<FieldMeta> Fields { get; init; } = [];
}
