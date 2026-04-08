namespace Alfred.Identity.Domain.Querying;

/// <summary>
/// Marker interface for operation filter input types (StringFilterInput, BoolFilterInput, etc.).
/// Each type exposes typed operator properties that Swagger can describe,
/// then converts to the internal <see cref="FieldOperation"/> list for expression binding.
/// </summary>
public interface IOperationFilterInput
{
    IReadOnlyList<FieldOperation> ToOperations();
}

/// <summary>
/// Marker interface for collection filter input types (CollectionFilterInput&lt;T&gt;).
/// Converts to a <see cref="CollectionFilterNode"/> for the expression binder pipeline.
/// </summary>
public interface ICollectionFilterInput
{
    CollectionFilterNode? ToCollectionFilterNode(string fieldName);
}

/// <summary>
/// Collection field filter with inner predicate operators: <c>some</c>, <c>all</c>, <c>none</c>.
/// <para>
/// Example JSON: <c>{ "roles": { "some": { "name": { "eq": "Admin" } } } }</c>
/// </para>
/// </summary>
/// <typeparam name="TInner">The filter input type for the inner predicate.</typeparam>
public sealed class CollectionFilterInput<TInner> : ICollectionFilterInput
    where TInner : IFilterInput
{
    /// <summary>At least one collection element must match the inner filter.</summary>
    public TInner? Some { get; set; }

    /// <summary>All collection elements must match the inner filter.</summary>
    public TInner? All { get; set; }

    /// <summary>No collection element must match the inner filter.</summary>
    public TInner? None { get; set; }

    public CollectionFilterNode? ToCollectionFilterNode(string fieldName)
    {
        if (Some is not null)
        {
            var inner = Some.ToFilterNode();
            return inner is null ? null : new CollectionFilterNode(fieldName, CollectionOperator.Some, inner);
        }

        if (All is not null)
        {
            var inner = All.ToFilterNode();
            return inner is null ? null : new CollectionFilterNode(fieldName, CollectionOperator.All, inner);
        }

        if (None is not null)
        {
            var inner = None.ToFilterNode();
            return inner is null ? null : new CollectionFilterNode(fieldName, CollectionOperator.None, inner);
        }

        return null;
    }
}

/// <summary>
/// String field filter operators.
/// Example JSON: { "email": { "contains": "admin", "neq": "test@example.com" } }
/// </summary>
public sealed class StringFilterInput : IOperationFilterInput
{
    public string? Eq { get; set; }
    public string? Neq { get; set; }
    public string? Contains { get; set; }
    public string? Ncontains { get; set; }
    public string? StartsWith { get; set; }
    public string? NstartsWith { get; set; }
    public string? EndsWith { get; set; }
    public string? NendsWith { get; set; }
    public List<string>? In { get; set; }
    public List<string>? Nin { get; set; }

    public IReadOnlyList<FieldOperation> ToOperations()
    {
        var ops = new List<FieldOperation>();
        if (Eq is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Eq, Eq));
        }

        if (Neq is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Neq, Neq));
        }

        if (Contains is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Contains, Contains));
        }

        if (Ncontains is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Ncontains, Ncontains));
        }

        if (StartsWith is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.StartsWith, StartsWith));
        }

        if (NstartsWith is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.NstartsWith, NstartsWith));
        }

        if (EndsWith is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.EndsWith, EndsWith));
        }

        if (NendsWith is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.NendsWith, NendsWith));
        }

        if (In is { Count: > 0 })
        {
            ops.Add(new FieldOperation(ComparisonOperators.In, In.Cast<object?>().ToList()));
        }

        if (Nin is { Count: > 0 })
        {
            ops.Add(new FieldOperation(ComparisonOperators.Nin, Nin.Cast<object?>().ToList()));
        }

        return ops;
    }
}

/// <summary>
/// Boolean field filter operators.
/// Example JSON: { "isActive": { "eq": true } }
/// </summary>
public sealed class BoolFilterInput : IOperationFilterInput
{
    public bool? Eq { get; set; }
    public bool? Neq { get; set; }

    public IReadOnlyList<FieldOperation> ToOperations()
    {
        var ops = new List<FieldOperation>();
        if (Eq is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Eq, Eq));
        }

        if (Neq is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Neq, Neq));
        }

        return ops;
    }
}

/// <summary>
/// DateTime/DateOnly field filter operators.
/// Example JSON: { "createdAt": { "gte": "2024-01-01", "lt": "2025-01-01" } }
/// </summary>
public sealed class DateTimeFilterInput : IOperationFilterInput
{
    public string? Eq { get; set; }
    public string? Neq { get; set; }
    public string? Gt { get; set; }
    public string? Gte { get; set; }
    public string? Lt { get; set; }
    public string? Lte { get; set; }

    public IReadOnlyList<FieldOperation> ToOperations()
    {
        var ops = new List<FieldOperation>();
        if (Eq is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Eq, Eq));
        }

        if (Neq is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Neq, Neq));
        }

        if (Gt is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Gt, Gt));
        }

        if (Gte is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Gte, Gte));
        }

        if (Lt is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Lt, Lt));
        }

        if (Lte is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Lte, Lte));
        }

        return ops;
    }
}

/// <summary>
/// Guid/ID field filter operators.
/// Example JSON: { "id": { "eq": "550e8400-e29b-41d4-a716-446655440000" } }
/// </summary>
public sealed class GuidFilterInput : IOperationFilterInput
{
    public string? Eq { get; set; }
    public string? Neq { get; set; }
    public List<string>? In { get; set; }
    public List<string>? Nin { get; set; }

    public IReadOnlyList<FieldOperation> ToOperations()
    {
        var ops = new List<FieldOperation>();
        if (Eq is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Eq, Eq));
        }

        if (Neq is not null)
        {
            ops.Add(new FieldOperation(ComparisonOperators.Neq, Neq));
        }

        if (In is { Count: > 0 })
        {
            ops.Add(new FieldOperation(ComparisonOperators.In, In.Cast<object?>().ToList()));
        }

        if (Nin is { Count: > 0 })
        {
            ops.Add(new FieldOperation(ComparisonOperators.Nin, Nin.Cast<object?>().ToList()));
        }

        return ops;
    }
}
