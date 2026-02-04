using System.Linq.Expressions;

namespace Alfred.Identity.Application.Querying.Common;

/// <summary>
/// Shared expression visitor to replace parameter references in lambda expressions.
/// Used by EfFilterBinder, SortBinder, ProjectionBinder, and ExpressionConverterHelper.
/// </summary>
public sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter ?? throw new ArgumentNullException(nameof(oldParameter));
        _newParameter = newParameter ?? throw new ArgumentNullException(nameof(newParameter));
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }

    /// <summary>
    /// Replace the parameter in the given expression body
    /// </summary>
    public static Expression Replace(Expression body, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        var replacer = new ParameterReplacer(oldParameter, newParameter);
        return replacer.Visit(body);
    }

    /// <summary>
    /// Replace the parameter in a lambda expression and return the new body
    /// </summary>
    public static Expression ReplaceIn(LambdaExpression lambda, ParameterExpression newParameter)
    {
        var replacer = new ParameterReplacer(lambda.Parameters[0], newParameter);
        return replacer.Visit(lambda.Body);
    }
}
