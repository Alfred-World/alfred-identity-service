using System.Linq.Expressions;

using Alfred.Identity.Application.Querying.Validation;

namespace Alfred.Identity.Application.Querying.Binding;

/// <summary>
/// Helper để apply sorting cho EF Core
/// </summary>
public static class SortBinder
{
    /// <summary>
    /// Apply sorting. Format: "-createdAt,name" (- prefix means descending)
    /// </summary>
    public static IQueryable<T> ApplySort<T>(IQueryable<T> query, string? sort, FieldMap<T> fieldMap)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return query;
        }

        var sortParts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var sortPart in sortParts)
        {
            var trimmed = sortPart.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            var descending = trimmed.StartsWith('-');
            var fieldName = descending ? trimmed[1..] : trimmed;

            // Check if user accidentally put a filter expression in sort parameter
            if (fieldName.Contains(' ') || fieldName.Contains('(') || fieldName.Contains('@'))
            {
                throw new InvalidOperationException(
                    $"Invalid sort field '{fieldName}'. " +
                    "Sort parameter should contain field names only (e.g., 'username', '-createdAt'). " +
                    "Did you mean to use the 'filter' parameter instead?");
            }

            if (!fieldMap.TryGet(fieldName, out var expression, out _))
            {
                throw new InvalidOperationException($"Field '{fieldName}' not found for sorting");
            }

            if (!fieldMap.CanSort(fieldName))
            {
                throw new InvalidOperationException($"Field '{fieldName}' cannot be used for sorting");
            }

            // Cast to Expression<Func<T, object>> for sorting
            var parameter = Expression.Parameter(typeof(T), "x");
            ParameterReplacerVisitor visitor = new(expression.Parameters[0], parameter);
            var body = visitor.Visit(expression.Body);

            // Box value types to object for sorting
            if (body.Type.IsValueType)
            {
                body = Expression.Convert(body, typeof(object));
            }

            var lambda = Expression.Lambda<Func<T, object>>(body, parameter);

            orderedQuery = orderedQuery == null
                ? descending ? query.OrderByDescending(lambda) : query.OrderBy(lambda)
                : descending
                    ? orderedQuery.ThenByDescending(lambda)
                    : orderedQuery.ThenBy(lambda);
        }

        return orderedQuery ?? query;
    }

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
