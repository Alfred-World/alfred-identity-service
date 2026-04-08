using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

using Alfred.Identity.Domain.Querying;

namespace Alfred.Identity.Infrastructure.Querying;

/// <summary>
/// Binds a JSON FilterNode tree into an Expression&lt;Func&lt;T, bool&gt;&gt; for EF Core.
/// Lives in Infrastructure because it produces EF-translatable expression trees.
/// </summary>
public static class FilterExpressionBinder<T> where T : class
{
    public static Expression<Func<T, bool>> Bind(FilterNode node, IFieldResolver<T> fieldResolver)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var body = BindNode(node, parameter, fieldResolver);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private static Expression BindNode(FilterNode node, ParameterExpression parameter, IFieldResolver<T> fieldResolver)
    {
        return node switch
        {
            LogicalFilterNode logical => BindLogical(logical, parameter, fieldResolver),
            FieldFilterNode field => BindField(field, parameter, fieldResolver),
            CollectionFilterNode collection => BindCollection(collection, parameter, fieldResolver),
            _ => throw new NotSupportedException($"Unknown filter node type: {node.GetType().Name}")
        };
    }

    private static Expression BindLogical(
        LogicalFilterNode node, ParameterExpression parameter, IFieldResolver<T> fieldResolver)
    {
        if (node.Conditions.Count == 0)
        {
            return Expression.Constant(true);
        }

        var first = BindNode(node.Conditions[0], parameter, fieldResolver);

        return node.Operator switch
        {
            LogicalOperator.And => node.Conditions.Skip(1)
                .Aggregate(first, (acc, c) => Expression.AndAlso(acc, BindNode(c, parameter, fieldResolver))),
            LogicalOperator.Or => node.Conditions.Skip(1)
                .Aggregate(first, (acc, c) => Expression.OrElse(acc, BindNode(c, parameter, fieldResolver))),
            _ => throw new NotSupportedException($"Unknown logical operator: {node.Operator}")
        };
    }

    private static Expression BindField(
        FieldFilterNode node, ParameterExpression parameter, IFieldResolver<T> fieldResolver)
    {
        if (!fieldResolver.CanFilter(node.FieldName))
        {
            throw new InvalidOperationException($"Field '{node.FieldName}' is not filterable");
        }

        if (!fieldResolver.TryResolve(node.FieldName, out var expression, out _))
        {
            throw new InvalidOperationException($"Field '{node.FieldName}' not found");
        }

        var fieldExpr = ReplaceParameter(expression, parameter);

        if (node.Operations.Count == 1)
        {
            return BindOperation(fieldExpr, node.Operations[0]);
        }

        // Multiple operations on same field → implicit AND
        return node.Operations
            .Select(op => BindOperation(fieldExpr, op))
            .Aggregate(Expression.AndAlso);
    }

    private static Expression BindCollection(
        CollectionFilterNode node, ParameterExpression parameter, IFieldResolver<T> fieldResolver)
    {
        if (!fieldResolver.CanFilter(node.FieldName))
        {
            throw new InvalidOperationException($"Field '{node.FieldName}' is not filterable");
        }

        // Prefer the filter-specific expression when available (e.g. raw navigation for collection fields)
        // so EF Core can translate inner predicates to SQL without going through DTO projections.
        if (!fieldResolver.TryResolveForFilter(node.FieldName, out var expression, out var propertyType))
        {
            throw new InvalidOperationException($"Collection field '{node.FieldName}' not found");
        }

        var collectionExpr = ReplaceParameter(expression, parameter);

        // Get element type from IEnumerable<TElement>
        var elementType = GetCollectionElementType(propertyType)
                          ?? throw new InvalidOperationException(
                              $"Field '{node.FieldName}' is not a collection type");

        return node.Operator switch
        {
            CollectionOperator.Any => BuildCollectionAny(collectionExpr, elementType),
            CollectionOperator.Some => BuildCollectionPredicate(
                collectionExpr, elementType, node.InnerFilter, nameof(Enumerable.Any), node.FieldName, fieldResolver),
            CollectionOperator.All => BuildCollectionPredicate(
                collectionExpr, elementType, node.InnerFilter, nameof(Enumerable.All), node.FieldName, fieldResolver),
            CollectionOperator.None => Expression.Not(BuildCollectionPredicate(
                collectionExpr, elementType, node.InnerFilter, nameof(Enumerable.Any), node.FieldName, fieldResolver)),
            _ => throw new NotSupportedException($"Unknown collection operator: {node.Operator}")
        };
    }

    private static Expression BuildCollectionAny(Expression collection, Type elementType)
    {
        // collection.Any() — checks if collection has elements
        var anyMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 1)
            .MakeGenericMethod(elementType);

        return Expression.Call(anyMethod, collection);
    }

    private static Expression BuildCollectionPredicate(
        Expression collection, Type elementType, FilterNode? innerFilter, string methodName,
        string collectionFieldName, IFieldResolver<T> fieldResolver)
    {
        if (innerFilter == null)
        {
            throw new InvalidOperationException($"Collection operator '{methodName}' requires an inner filter");
        }

        var allowedInner = fieldResolver.GetAllowedInnerFields(collectionFieldName);

        // Build inner predicate: element => <inner filter conditions>
        var innerParam = Expression.Parameter(elementType, "e");
        var innerBody = BindInnerFilterNode(innerFilter, innerParam, elementType, allowedInner);
        var predicate = Expression.Lambda(innerBody, innerParam);

        // collection.Any(e => ...) or collection.All(e => ...)
        var method = typeof(Enumerable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        return Expression.Call(method, collection, predicate);
    }

    /// <summary>
    /// Bind inner filter for collection elements using direct property access (no field map).
    /// </summary>
    private static Expression BindInnerFilterNode(
        FilterNode node, ParameterExpression parameter, Type elementType, IReadOnlySet<string>? allowedInner)
    {
        return node switch
        {
            LogicalFilterNode logical => BindInnerLogical(logical, parameter, elementType, allowedInner),
            FieldFilterNode field => BindInnerField(field, parameter, elementType, allowedInner),
            _ => throw new NotSupportedException(
                $"Unsupported inner filter node type: {node.GetType().Name}")
        };
    }

    private static Expression BindInnerLogical(
        LogicalFilterNode node, ParameterExpression parameter, Type elementType, IReadOnlySet<string>? allowedInner)
    {
        if (node.Conditions.Count == 0)
        {
            return Expression.Constant(true);
        }

        var first = BindInnerFilterNode(node.Conditions[0], parameter, elementType, allowedInner);

        return node.Operator switch
        {
            LogicalOperator.And => node.Conditions.Skip(1)
                .Aggregate(first,
                    (acc, c) => Expression.AndAlso(acc, BindInnerFilterNode(c, parameter, elementType, allowedInner))),
            LogicalOperator.Or => node.Conditions.Skip(1)
                .Aggregate(first,
                    (acc, c) => Expression.OrElse(acc, BindInnerFilterNode(c, parameter, elementType, allowedInner))),
            _ => throw new NotSupportedException($"Unknown logical operator: {node.Operator}")
        };
    }

    private static Expression BindInnerField(
        FieldFilterNode node, ParameterExpression parameter, Type elementType, IReadOnlySet<string>? allowedInner)
    {
        // If the parent collection field declared an AllowedInnerFields whitelist, enforce it.
        // This keeps inner filter control inside the FieldMap (Application layer) not the binder.
        if (allowedInner != null && !allowedInner.Contains(node.FieldName))
        {
            throw new InvalidOperationException(
                $"Field '{node.FieldName}' is not available for collection filtering");
        }

        // Direct property access on the element type.
        // Security: only allow scalar/primitive properties to prevent accidental exposure of
        // navigation properties or sensitive fields (e.g. PasswordHash on related entities).
        var property = elementType.GetProperty(node.FieldName,
                           BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                       ?? throw new InvalidOperationException(
                           $"Field '{node.FieldName}' is not available for collection filtering");

        if (!IsAllowedInnerPropertyType(property.PropertyType))
        {
            throw new InvalidOperationException(
                $"Field '{node.FieldName}' is not available for collection filtering");
        }

        var propertyExpr = Expression.Property(parameter, property);

        if (node.Operations.Count == 1)
        {
            return BindOperation(propertyExpr, node.Operations[0]);
        }

        return node.Operations
            .Select(op => BindOperation(propertyExpr, op))
            .Aggregate(Expression.AndAlso);
    }

    /// <summary>
    /// Only allow scalar/primitive types in inner collection predicates.
    /// This prevents navigation properties, byte arrays, and other complex types
    /// from being accessed via collection filters.
    /// </summary>
    private static bool IsAllowedInnerPropertyType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive
               || t == typeof(string)
               || t == typeof(Guid)
               || t == typeof(DateTime)
               || t == typeof(DateTimeOffset)
               || t == typeof(DateOnly)
               || t == typeof(TimeOnly)
               || t == typeof(decimal)
               || t.IsEnum;
    }

    #region Operation Binding

    private static Expression BindOperation(Expression target, FieldOperation op)
    {
        var opName = op.Operator.ToLowerInvariant();

        return opName switch
        {
            "eq" => BuildEqual(target, op.Value),
            "neq" => BuildNotEqual(target, op.Value),
            "gt" => BuildComparison(target, op.Value, Expression.GreaterThan),
            "gte" => BuildComparison(target, op.Value, Expression.GreaterThanOrEqual),
            "lt" => BuildComparison(target, op.Value, Expression.LessThan),
            "lte" => BuildComparison(target, op.Value, Expression.LessThanOrEqual),
            "in" => BuildIn(target, op.Value),
            "nin" => Expression.Not(BuildIn(target, op.Value)),
            "contains" => BuildStringMethod(target, op.Value, nameof(string.Contains)),
            "ncontains" => Expression.Not(BuildStringMethod(target, op.Value, nameof(string.Contains))),
            "startswith" => BuildStringMethod(target, op.Value, nameof(string.StartsWith)),
            "nstartswith" => Expression.Not(BuildStringMethod(target, op.Value, nameof(string.StartsWith))),
            "endswith" => BuildStringMethod(target, op.Value, nameof(string.EndsWith)),
            "nendswith" => Expression.Not(BuildStringMethod(target, op.Value, nameof(string.EndsWith))),
            _ => throw new NotSupportedException($"Unknown operator: '{op.Operator}'")
        };
    }

    #endregion

    #region Expression Builders

    private static Expression BuildEqual(Expression target, object? value)
    {
        if (value == null)
        {
            return Expression.Equal(target, Expression.Constant(null, target.Type));
        }

        var constant = CoerceConstant(value, target.Type);
        return Expression.Equal(target, constant);
    }

    private static Expression BuildNotEqual(Expression target, object? value)
    {
        if (value == null)
        {
            return Expression.NotEqual(target, Expression.Constant(null, target.Type));
        }

        var constant = CoerceConstant(value, target.Type);
        return Expression.NotEqual(target, constant);
    }

    private static Expression BuildComparison(
        Expression target, object? value,
        Func<Expression, Expression, Expression> comparator)
    {
        if (value == null)
        {
            throw new InvalidOperationException("Comparison operators do not support null values");
        }

        var constant = CoerceConstant(value, target.Type);
        return comparator(target, constant);
    }

    private static Expression BuildIn(Expression target, object? value)
    {
        if (value is not IList<object?> list || list.Count == 0)
        {
            return Expression.Constant(false);
        }

        var conditions = list
            .Select(v => BuildEqual(target, v))
            .ToList();

        return conditions.Aggregate(Expression.OrElse);
    }

    private static Expression BuildStringMethod(Expression target, object? value, string methodName)
    {
        if (value is not string strValue)
        {
            throw new InvalidOperationException($"String operator '{methodName}' requires a string value");
        }

        // Null check: target != null
        var nullCheck = Expression.NotEqual(target, Expression.Constant(null, target.Type));

        var method = typeof(string).GetMethod(methodName, [typeof(string)])
                     ?? throw new InvalidOperationException($"Method {methodName} not found on string");

        // Case-insensitive: ToLower() on both sides (PostgreSQL-friendly)
        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var lowerTarget = Expression.Call(target, toLowerMethod);
        var lowerArg = Expression.Call(Expression.Constant(strValue), toLowerMethod);

        var call = Expression.Call(lowerTarget, method, lowerArg);
        return Expression.AndAlso(nullCheck, call);
    }

    #endregion

    #region Type Coercion

    private static ConstantExpression CoerceConstant(object value, Type targetType)
    {
        var underlyingTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Already the right type
        if (value.GetType() == underlyingTarget)
        {
            return Expression.Constant(value, targetType);
        }

        // String to DateOnly
        if (underlyingTarget == typeof(DateOnly) && value is string dateStr)
        {
            if (DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return Expression.Constant(date, targetType);
            }

            throw new InvalidOperationException($"Cannot convert '{dateStr}' to DateOnly. Use format: yyyy-MM-dd");
        }

        // String to DateTime
        if (underlyingTarget == typeof(DateTime) && value is string dtStr)
        {
            if (DateTime.TryParse(dtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            {
                return Expression.Constant(dt, targetType);
            }

            throw new InvalidOperationException($"Cannot convert '{dtStr}' to DateTime");
        }

        // String to DateTimeOffset
        if (underlyingTarget == typeof(DateTimeOffset) && value is string dtoStr)
        {
            if (DateTimeOffset.TryParse(dtoStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                    out var dto))
            {
                return Expression.Constant(dto, targetType);
            }

            throw new InvalidOperationException($"Cannot convert '{dtoStr}' to DateTimeOffset");
        }

        // String to Guid
        if (underlyingTarget == typeof(Guid) && value is string guidStr)
        {
            if (Guid.TryParse(guidStr, out var guid))
            {
                return Expression.Constant(guid, targetType);
            }

            throw new InvalidOperationException($"Cannot convert '{guidStr}' to Guid");
        }

        // String/int to Enum
        if (underlyingTarget.IsEnum)
        {
            return value switch
            {
                string s when Enum.TryParse(underlyingTarget, s, true, out var enumVal)
                    => Expression.Constant(enumVal, targetType),
                string s => throw new InvalidOperationException(
                    $"'{s}' is not a valid value for {underlyingTarget.Name}. " +
                    $"Valid values: {string.Join(", ", Enum.GetNames(underlyingTarget))}"),
                _ when Enum.IsDefined(underlyingTarget,
                        Convert.ChangeType(value, Enum.GetUnderlyingType(underlyingTarget)))
                    => Expression.Constant(Enum.ToObject(underlyingTarget, value), targetType),
                _ => throw new InvalidOperationException(
                    $"'{value}' is not a valid value for {underlyingTarget.Name}")
            };
        }

        // Numeric conversions (JSON numbers come as long or double)
        try
        {
            var converted = Convert.ChangeType(value, underlyingTarget, CultureInfo.InvariantCulture);
            return Expression.Constant(converted, targetType);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot convert value '{value}' ({value.GetType().Name}) to {underlyingTarget.Name}: {ex.Message}");
        }
    }

    #endregion

    #region Helpers

    private static Expression ReplaceParameter(LambdaExpression lambda, ParameterExpression newParameter)
    {
        var oldParam = lambda.Parameters[0];
        var visitor = new ParameterReplacerVisitor(oldParam, newParameter);
        return visitor.Visit(lambda.Body);
    }

    private static Type? GetCollectionElementType(Type type)
    {
        // Check if the type itself is IEnumerable<T> (interfaces don't list themselves)
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        // Check IEnumerable<T> implemented by a class/struct (e.g. List<T>, ICollection<T>)
        var enumerable = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerable?.GetGenericArguments()[0];
    }

    /// <summary>
    /// Replaces parameter references in expression trees.
    /// Self-contained here so Infrastructure doesn't depend on Application's ParameterReplacer.
    /// </summary>
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

    #endregion
}
