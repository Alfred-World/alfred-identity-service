namespace Alfred.Identity.Application.Querying.Filtering.Ast;

/// <summary>
/// Base class cho các node trong AST của filter
/// </summary>
public abstract record FilterNode
{
    public abstract int Depth { get; }
}

/// <summary>
/// Binary operation node (AND, OR, ==, !=, >, <, etc.)
/// </summary>
public sealed record BinaryNode(FilterOperator Operator, FilterNode Left, FilterNode Right) : FilterNode
{
    public override int Depth => 1 + Math.Max(Left.Depth, Right.Depth);
}

/// <summary>
/// Unary operation node (NOT, @isnull, @notnull)
/// </summary>
public sealed record UnaryNode(FilterOperator Operator, FilterNode Operand) : FilterNode
{
    public override int Depth => 1 + Operand.Depth;
}

/// <summary>
/// Function call node (@contains, @startswith, @in, @between, etc.)
/// </summary>
public sealed record CallNode(FilterOperator Operator, FilterNode Target, IReadOnlyList<FilterNode> Arguments)
    : FilterNode
{
    public override int Depth => 1 + Math.Max(Target.Depth, Arguments.Any() ? Arguments.Max(a => a.Depth) : 0);
}

/// <summary>
/// Field reference node (username, email, price, etc.)
/// </summary>
public sealed record FieldNode(string Name) : FilterNode
{
    public override int Depth => 1;
}

/// <summary>
/// Literal value node (string, number, boolean, null)
/// </summary>
public sealed record LiteralNode(object? Value, Type Type) : FilterNode
{
    public override int Depth => 1;

    public static LiteralNode String(string value)
    {
        return new LiteralNode(value, typeof(string));
    }

    public static LiteralNode Number(double value)
    {
        return new LiteralNode(value, typeof(double));
    }

    public static LiteralNode Integer(long value)
    {
        return new LiteralNode(value, typeof(long));
    }

    public static LiteralNode Boolean(bool value)
    {
        return new LiteralNode(value, typeof(bool));
    }

    public static LiteralNode Null()
    {
        return new LiteralNode(null, typeof(object));
    }
}

/// <summary>
/// Group node (parentheses)
/// </summary>
public sealed record GroupNode(FilterNode Expression) : FilterNode
{
    public override int Depth => Expression.Depth;
}
