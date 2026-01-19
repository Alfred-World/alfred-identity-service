using System.Linq.Expressions;

using Alfred.Identity.Application.Querying.Validation;

namespace Alfred.Identity.Application.Querying;

/// <summary>
/// Base class for field maps with common include parsing logic
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public abstract class BaseFieldMap<TEntity> where TEntity : class
{
    /// <summary>
    /// Field map instance for filtering, sorting, and querying
    /// </summary>
    public abstract FieldMap<TEntity> Fields { get; }

    /// <summary>
    /// Dictionary of allowed includes with their corresponding expressions
    /// </summary>
    protected abstract Dictionary<string, Expression<Func<TEntity, object>>> AllowedIncludes { get; }

    /// <summary>
    /// Parse include string and return corresponding expressions
    /// Throws InvalidOperationException if include is not allowed
    /// </summary>
    public Expression<Func<TEntity, object>>[] ParseIncludes(string? includeString)
    {
        if (string.IsNullOrWhiteSpace(includeString))
        {
            return Array.Empty<Expression<Func<TEntity, object>>>();
        }

        var includeNames = includeString
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        List<Expression<Func<TEntity, object>>> expressions = new();

        foreach (var includeName in includeNames)
        {
            if (AllowedIncludes.TryGetValue(includeName, out var expression))
            {
                expressions.Add(expression);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Include '{includeName}' is not allowed. Allowed includes: {string.Join(", ", AllowedIncludes.Keys)}");
            }
        }

        return expressions.ToArray();
    }

    /// <summary>
    /// Get all allowed include names
    /// </summary>
    public IEnumerable<string> GetAllowedIncludeNames()
    {
        return AllowedIncludes.Keys;
    }

    /// <summary>
    /// Check if an include is allowed
    /// </summary>
    public bool IsIncludeAllowed(string includeName)
    {
        return AllowedIncludes.ContainsKey(includeName);
    }
}
