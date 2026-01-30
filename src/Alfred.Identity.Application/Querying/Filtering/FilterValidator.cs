using Alfred.Identity.Application.Querying.Fields;
using Alfred.Identity.Application.Querying.Filtering.Ast;

namespace Alfred.Identity.Application.Querying.Filtering;

/// <summary>
/// Validator cho filter AST
/// Checks: depth, node count, field whitelist, type safety, valid operators
/// </summary>
public static class FilterValidator
{
    private const int MaxDepth = 10;
    private const int MaxNodes = 50;

    public static void Validate<T>(FilterNode node, FieldMap<T> fieldMap)
    {
        // Check depth
        if (node.Depth > MaxDepth)
        {
            throw new InvalidOperationException($"Filter expression is too deep (max {MaxDepth} levels)");
        }

        // Check node count
        var nodeCount = CountNodes(node);
        if (nodeCount > MaxNodes)
        {
            throw new InvalidOperationException($"Filter expression is too complex (max {MaxNodes} nodes)");
        }

        // Validate fields and operators
        ValidateNode(node, fieldMap);
    }

    private static void ValidateNode<T>(FilterNode node, FieldMap<T> fieldMap)
    {
        switch (node)
        {
            case BinaryNode binary:
                ValidateNode(binary.Left, fieldMap);
                ValidateNode(binary.Right, fieldMap);
                ValidateBinaryOperator(binary, fieldMap);
                break;

            case UnaryNode unary:
                ValidateNode(unary.Operand, fieldMap);
                ValidateUnaryOperator(unary, fieldMap);
                break;

            case CallNode call:
                ValidateNode(call.Target, fieldMap);
                foreach (var arg in call.Arguments)
                {
                    ValidateNode(arg, fieldMap);
                }

                ValidateCallOperator(call, fieldMap);
                break;

            case FieldNode field:
                ValidateField(field, fieldMap);
                break;

            case GroupNode group:
                ValidateNode(group.Expression, fieldMap);
                break;

            case LiteralNode:
                // Literals are always valid
                break;

            default:
                throw new NotSupportedException($"Unknown node type: {node.GetType().Name}");
        }
    }

    private static void ValidateField<T>(FieldNode field, FieldMap<T> fieldMap)
    {
        if (!fieldMap.ContainsField(field.Name))
        {
            throw new InvalidOperationException($"Field '{field.Name}' is not allowed for filtering");
        }

        if (!fieldMap.CanFilter(field.Name))
        {
            throw new InvalidOperationException($"Field '{field.Name}' cannot be used in filters");
        }
    }

    private static void ValidateBinaryOperator<T>(BinaryNode node, FieldMap<T> fieldMap)
    {
        // Logic operators (and, or) don't need type checking
        if (node.Operator is FilterOperator.And or FilterOperator.Or)
        {
            return;
        }

        // Comparison operators need compatible types
        var leftType = GetNodeType(node.Left, fieldMap);
        var rightType = GetNodeType(node.Right, fieldMap);

        if (leftType == null || rightType == null)
        {
            return; // Can't validate without type info
        }

        // Check operator is valid for type
        ValidateOperatorForType(node.Operator, leftType);
    }

    private static void ValidateUnaryOperator<T>(UnaryNode node, FieldMap<T> fieldMap)
    {
        if (node.Operator == FilterOperator.Not)
        {
            return; // NOT can be applied to any boolean expression
        }

        var operandType = GetNodeType(node.Operand, fieldMap);
        if (operandType != null)
        {
            ValidateOperatorForType(node.Operator, operandType);
        }
    }

    private static void ValidateCallOperator<T>(CallNode node, FieldMap<T> fieldMap)
    {
        var targetType = GetNodeType(node.Target, fieldMap);
        if (targetType == null)
        {
            return;
        }

        ValidateOperatorForType(node.Operator, targetType);
    }

    private static Type? GetNodeType<T>(FilterNode node, FieldMap<T> fieldMap)
    {
        return node switch
        {
            FieldNode field when fieldMap.TryGet(field.Name, out _, out var type) => type,
            LiteralNode literal => literal.Type,
            _ => null
        };
    }

    private static void ValidateOperatorForType(FilterOperator op, Type type)
    {
        var isString = type == typeof(string);
        var isNumeric = IsNumericType(type);
        var isDateTime = type == typeof(DateTime) || type == typeof(DateTime?);
        var isBool = type == typeof(bool) || type == typeof(bool?);

        switch (op)
        {
            case FilterOperator.Contains:
            case FilterOperator.NotContains:
            case FilterOperator.StartsWith:
            case FilterOperator.EndsWith:
                if (!isString)
                {
                    throw new InvalidOperationException($"Operator '{op}' can only be used with string fields");
                }

                break;

            case FilterOperator.GreaterThan:
            case FilterOperator.GreaterThanOrEqual:
            case FilterOperator.LessThan:
            case FilterOperator.LessThanOrEqual:
            case FilterOperator.Between:
                if (!isNumeric && !isDateTime)
                {
                    throw new InvalidOperationException(
                        $"Operator '{op}' can only be used with numeric or date fields");
                }

                break;

            case FilterOperator.Equal:
            case FilterOperator.NotEqual:
            case FilterOperator.In:
            case FilterOperator.NotIn:
            case FilterOperator.IsNull:
            case FilterOperator.NotNull:
                // These can be used with any type
                break;

            default:
                // Unknown operator
                break;
        }
    }

    private static bool IsNumericType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType == typeof(int)
               || underlyingType == typeof(long)
               || underlyingType == typeof(short)
               || underlyingType == typeof(byte)
               || underlyingType == typeof(uint)
               || underlyingType == typeof(ulong)
               || underlyingType == typeof(ushort)
               || underlyingType == typeof(sbyte)
               || underlyingType == typeof(float)
               || underlyingType == typeof(double)
               || underlyingType == typeof(decimal);
    }

    private static int CountNodes(FilterNode node)
    {
        return node switch
        {
            BinaryNode binary => 1 + CountNodes(binary.Left) + CountNodes(binary.Right),
            UnaryNode unary => 1 + CountNodes(unary.Operand),
            CallNode call => 1 + CountNodes(call.Target) + call.Arguments.Sum(CountNodes),
            GroupNode group => CountNodes(group.Expression),
            _ => 1
        };
    }
}
