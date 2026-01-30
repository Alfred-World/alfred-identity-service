using System.Linq.Expressions;

namespace Alfred.Identity.Application.Querying.Fields;

/// <summary>
/// Helper to extract field names from lambda expressions
/// </summary>
public static class FieldExpressionHelper
{
    /// <summary>
    /// Extract camelCase property name from expression (e.g., x => x.Id -> "id")
    /// </summary>
    public static string GetFieldName<T>(Expression<Func<T, object?>> expression)
    {
        var memberExpression = GetMemberExpression(expression);
        if (memberExpression == null)
        {
            throw new ArgumentException("Expression must be a member access expression", nameof(expression));
        }

        var propertyName = memberExpression.Member.Name;
        return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }

    /// <summary>
    /// Convert multiple expressions to camelCase strings
    /// </summary>
    public static string[] GetFieldNames<T>(params Expression<Func<T, object?>>[] expressions)
    {
        return expressions.Select(GetFieldName).ToArray();
    }

    private static MemberExpression? GetMemberExpression<T>(Expression<Func<T, object?>> expression)
    {
        if (expression.Body is MemberExpression member)
        {
            return member;
        }

        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
        {
            return unaryMember;
        }

        return null;
    }
}
