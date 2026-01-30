using System.Linq.Expressions;

namespace Alfred.Identity.Application.Querying.Projection;

/// <summary>
/// Defines a named view with its allowed fields for projection
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
    /// Navigation properties to include (for nested field access)
    /// </summary>
    public Expression<Func<TEntity, object>>[]? Includes { get; }

    public ViewDefinition(
        string name,
        string[] fields,
        Expression<Func<TEntity, object>>[]? includes = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        Includes = includes;
    }
}
