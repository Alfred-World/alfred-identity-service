using System.Linq.Expressions;

namespace Alfred.Identity.Application.Querying.Projection;

/// <summary>
/// Defines a named view with its allowed fields for projection.
/// Supports both simple fields and field aliases (e.g., mapping "permissions" to "permissionsSummary").
/// </summary>
/// <typeparam name="TEntity">Source entity type</typeparam>
/// <typeparam name="TDto">Target DTO type</typeparam>
public sealed class ViewDefinition<TEntity, TDto>
    where TEntity : class
    where TDto : class, new()
{
    /// <summary>
    /// Name of the view (e.g., "list", "detail", "minimal")
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Fields allowed in this view (camelCase names matching FieldMap keys)
    /// </summary>
    public string[] Fields { get; }

    /// <summary>
    /// Field aliases mapping DTO property names to FieldMap keys.
    /// Example: { "permissions" -> "permissionsSummary" } means when projecting
    /// the "permissions" DTO property, use "permissionsSummary" from FieldMap.
    /// </summary>
    public IReadOnlyDictionary<string, string> FieldAliases { get; }

    /// <summary>
    /// Navigation properties to include (for nested field access)
    /// </summary>
    public Expression<Func<TEntity, object>>[]? Includes { get; }

    public ViewDefinition(
        string name,
        string[] fields,
        Expression<Func<TEntity, object>>[]? includes = null,
        Dictionary<string, string>? fieldAliases = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        Includes = includes;
        FieldAliases = fieldAliases ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Get the actual FieldMap key for a given DTO field name.
    /// Returns the alias if defined, otherwise returns the field name as-is.
    /// </summary>
    public string GetFieldMapKey(string dtoFieldName)
    {
        return FieldAliases.TryGetValue(dtoFieldName, out var alias) ? alias : dtoFieldName;
    }
}
