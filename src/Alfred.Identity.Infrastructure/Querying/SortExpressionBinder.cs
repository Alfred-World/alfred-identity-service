using System.Linq.Expressions;

using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Infrastructure.Querying;

/// <summary>
/// Converts SortField list into OrderBy/ThenBy expression chains on IQueryable.
/// Lives in Infrastructure because it builds EF-translatable expression trees.
/// </summary>
public static class SortExpressionBinder<T> where T : class
{
    /// <summary>
    /// Apply sort fields to the query. Falls back to "Id" ascending if no valid sort fields.
    /// </summary>
    public static IQueryable<T> Apply(
        IQueryable<T> query,
        IReadOnlyList<SortField>? sortFields,
        IFieldResolver<T> fieldResolver)
    {
        if (sortFields == null || sortFields.Count == 0)
        {
            return ApplyDefaultSort(query);
        }

        IOrderedQueryable<T>? ordered = null;

        foreach (var sortField in sortFields)
        {
            if (string.IsNullOrWhiteSpace(sortField.Field))
            {
                continue;
            }

            if (!fieldResolver.CanSort(sortField.Field))
            {
                throw new InvalidOperationException($"Field '{sortField.Field}' is not sortable");
            }

            if (!fieldResolver.TryResolve(sortField.Field, out var expression, out _))
            {
                throw new InvalidOperationException($"Sort field '{sortField.Field}' not found");
            }

            var objectExpression = ConvertToObjectExpression(expression);

            if (ordered == null)
            {
                ordered = sortField.Direction == SortDirection.Desc
                    ? query.OrderByDescending(objectExpression)
                    : query.OrderBy(objectExpression);
            }
            else
            {
                ordered = sortField.Direction == SortDirection.Desc
                    ? ordered.ThenByDescending(objectExpression)
                    : ordered.ThenBy(objectExpression);
            }
        }

        return ordered ?? ApplyDefaultSort(query);
    }

    private static IOrderedQueryable<T> ApplyDefaultSort(IQueryable<T> query)
    {
        // Default sort by Id ascending using dynamic LINQ
        var parameter = Expression.Parameter(typeof(T), "x");
        var idProperty = typeof(T).GetProperty("Id");

        if (idProperty == null)
        {
            throw new InvalidOperationException($"Entity type '{typeof(T).Name}' does not have an 'Id' property");
        }

        var propertyAccess = Expression.Property(parameter, idProperty);
        var converted = Expression.Convert(propertyAccess, typeof(object));
        var lambda = Expression.Lambda<Func<T, object>>(converted, parameter);

        return query.OrderBy(lambda);
    }

    private static Expression<Func<T, object>> ConvertToObjectExpression(LambdaExpression expression)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var body = ReplaceParameter(expression, parameter);

        // Box value types
        var converted = body.Type.IsValueType
            ? Expression.Convert(body, typeof(object))
            : body;

        return Expression.Lambda<Func<T, object>>(converted, parameter);
    }

    private static Expression ReplaceParameter(LambdaExpression lambda, ParameterExpression newParameter)
    {
        var oldParam = lambda.Parameters[0];
        if (oldParam == newParameter)
        {
            return lambda.Body;
        }

        var visitor = new ParameterReplacerVisitor(oldParam, newParameter);
        return visitor.Visit(lambda.Body);
    }

    private sealed class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _old;
        private readonly ParameterExpression _new;

        public ParameterReplacerVisitor(ParameterExpression old, ParameterExpression @new)
        {
            _old = old;
            _new = @new;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _old ? _new : base.VisitParameter(node);
        }
    }
}
