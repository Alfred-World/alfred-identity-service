using System.Linq.Expressions;

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
        var paramReplacer = new ParameterReplacerVisitor(expression.Parameters[0], parameter);
        var body = paramReplacer.Visit(expression.Body);

        // Box value types to object
        if (body.Type.IsValueType)
        {
            body = Expression.Convert(body, typeof(object));
        }

        return Expression.Lambda<Func<T, object>>(body, parameter);
    }

    /// <summary>
    /// Helper visitor to replace parameter references in expressions
    /// </summary>
    private class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacerVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
