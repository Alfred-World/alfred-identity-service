using System.Linq.Expressions;

using Alfred.Identity.Application.Querying.Common;

namespace Alfred.Identity.Application.Querying.Fields;

/// <summary>
/// Helper to convert LambdaExpressions to Expression<Func<T, object>> with proper boxing
/// </summary>
public static class ExpressionConverterHelper
{
    /// <summary>
    /// Convert any LambdaExpression to Expression<Func<T, object>> with boxing for value types
    /// </summary>
    public static Expression<Func<T, object>>? ConvertToObjectExpression<T>(LambdaExpression? expression)
        where T : class
    {
        if (expression == null)
        {
            return null;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var body = ParameterReplacer.ReplaceIn(expression, parameter);

        // Box value types to object
        if (body.Type.IsValueType)
        {
            body = Expression.Convert(body, typeof(object));
        }

        return Expression.Lambda<Func<T, object>>(body, parameter);
    }
}
