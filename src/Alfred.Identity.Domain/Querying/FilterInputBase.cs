using System.Reflection;
using System.Text.Json;

namespace Alfred.Identity.Domain.Querying;

/// <summary>
/// Interface for typed filter inputs that can convert to the internal <see cref="FilterNode"/> AST.
/// </summary>
public interface IFilterInput
{
    FilterNode? ToFilterNode();
}

/// <summary>
/// Base class for entity-specific typed filter inputs.
/// Provides AND/OR logical combinators and reflection-based conversion to <see cref="FilterNode"/>.
/// <para>
/// Subclasses declare strongly typed properties (e.g. <c>StringFilterInput? Email</c>)
/// which Swagger/OpenAPI can describe. At runtime, <see cref="ToFilterNode"/> walks the
/// properties via cached reflection and builds the equivalent <see cref="FilterNode"/> tree.
/// </para>
/// </summary>
/// <typeparam name="TSelf">The concrete filter input type (CRTP pattern for And/Or recursive typing).</typeparam>
public abstract class FilterInputBase<TSelf> : IFilterInput where TSelf : FilterInputBase<TSelf>
{
    /// <summary>All conditions must match.</summary>
    public List<TSelf>? And { get; set; }

    /// <summary>At least one condition must match.</summary>
    public List<TSelf>? Or { get; set; }

    // Cache filter properties per concrete type to avoid repeated reflection
    private static PropertyInfo[]? _cachedFilterProperties;
    private static PropertyInfo[]? _cachedCollectionFilterProperties;

    private static PropertyInfo[] GetFilterProperties()
    {
        return _cachedFilterProperties ??= typeof(TSelf)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name is not ("And" or "Or")
                        && typeof(IOperationFilterInput).IsAssignableFrom(p.PropertyType))
            .ToArray();
    }

    private static PropertyInfo[] GetCollectionFilterProperties()
    {
        return _cachedCollectionFilterProperties ??= typeof(TSelf)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name is not ("And" or "Or")
                        && typeof(ICollectionFilterInput).IsAssignableFrom(p.PropertyType))
            .ToArray();
    }

    /// <summary>
    /// Convert this typed filter input to the internal <see cref="FilterNode"/> AST
    /// used by the expression binder pipeline.
    /// </summary>
    public FilterNode? ToFilterNode()
    {
        var conditions = new List<FilterNode>();

        if (And is { Count: > 0 })
        {
            var innerConditions = And
                .Select(x => x.ToFilterNode())
                .Where(x => x is not null)
                .ToList();

            if (innerConditions.Count > 0)
            {
                conditions.Add(new LogicalFilterNode(LogicalOperator.And, innerConditions!));
            }
        }

        if (Or is { Count: > 0 })
        {
            var innerConditions = Or
                .Select(x => x.ToFilterNode())
                .Where(x => x is not null)
                .ToList();

            if (innerConditions.Count > 0)
            {
                conditions.Add(new LogicalFilterNode(LogicalOperator.Or, innerConditions!));
            }
        }

        foreach (var prop in GetFilterProperties())
        {
            if (prop.GetValue(this) is not IOperationFilterInput opInput)
            {
                continue;
            }

            var ops = opInput.ToOperations();
            if (ops.Count == 0)
            {
                continue;
            }

            // Convert PascalCase property name to camelCase field name (matching FieldMap keys)
            var fieldName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
            conditions.Add(new FieldFilterNode(fieldName, ops));
        }

        foreach (var prop in GetCollectionFilterProperties())
        {
            if (prop.GetValue(this) is not ICollectionFilterInput colInput)
            {
                continue;
            }

            var fieldName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
            var node = colInput.ToCollectionFilterNode(fieldName);
            if (node is not null)
            {
                conditions.Add(node);
            }
        }

        return conditions.Count switch
        {
            0 => null,
            1 => conditions[0],
            _ => new LogicalFilterNode(LogicalOperator.And, conditions)
        };
    }
}
