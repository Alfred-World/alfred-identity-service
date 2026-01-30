using System.Linq.Expressions;
using System.Reflection;

using Alfred.Identity.Application.Querying.Fields;

namespace Alfred.Identity.Application.Querying.Projection;

/// <summary>
/// Helper to apply field projection/selection to queries
/// Optimizes performance by selecting only requested fields from database
/// </summary>
public static class ProjectionBinder
{
    /// <summary>
    /// Apply field selection (projection) to query
    /// If fields is null/empty, returns all fields
    /// </summary>
    /// <typeparam name="TSource">Source entity type</typeparam>
    /// <typeparam name="TDto">DTO type to project to</typeparam>
    public static IQueryable<TDto> ApplyProjection<TSource, TDto>(
        IQueryable<TSource> query,
        string[]? fields,
        FieldMap<TSource> fieldMap)
        where TDto : class, new()
    {
        // If no fields specified, return all (will need manual mapping later)
        if (fields == null || fields.Length == 0)
        {
            throw new InvalidOperationException(
                "Cannot auto-project without field specification. " +
                "Either specify fields or use manual mapping.");
        }

        // Validate all requested fields exist and are selectable
        foreach (var field in fields)
        {
            if (!fieldMap.TryGet(field, out _, out _))
            {
                throw new InvalidOperationException($"Field '{field}' not found");
            }

            if (!fieldMap.CanSelect(field))
            {
                throw new InvalidOperationException($"Field '{field}' cannot be selected");
            }
        }

        // Build projection expression dynamically
        var parameter = Expression.Parameter(typeof(TSource), "x");
        var dtoType = typeof(TDto);

        // Create member bindings for each requested field
        List<MemberBinding> bindings = new();

        foreach (var fieldName in fields)
        {
            // Get source expression from field map
            if (!fieldMap.TryGet(fieldName, out var sourceExpression, out _))
            {
                continue;
            }

            // Find matching property in DTO
            var dtoProperty = dtoType.GetProperty(
                fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (dtoProperty == null || !dtoProperty.CanWrite)
            {
                continue;
            }

            // Replace parameter in source expression
            ParameterReplacerVisitor visitor = new(sourceExpression.Parameters[0], parameter);
            var sourceBody = visitor.Visit(sourceExpression.Body);

            // Convert if types don't match
            if (sourceBody.Type != dtoProperty.PropertyType)
            {
                if (dtoProperty.PropertyType.IsAssignableFrom(sourceBody.Type))
                {
                    sourceBody = Expression.Convert(sourceBody, dtoProperty.PropertyType);
                }
            }

            bindings.Add(Expression.Bind(dtoProperty, sourceBody));
        }

        // Create: x => new TDto { Field1 = x.Field1, Field2 = x.Field2, ... }
        var memberInit = Expression.MemberInit(Expression.New(dtoType), bindings);
        var lambda = Expression.Lambda<Func<TSource, TDto>>(memberInit, parameter);

        return query.Select(lambda);
    }

    /// <summary>
    /// Build a projection expression that selects specified fields into a Dictionary
    /// This is useful for dynamic field selection at database level
    /// </summary>
    /// <typeparam name="TSource">Source entity type</typeparam>
    /// <param name="fields">Fields to select (null/empty means all fields)</param>
    /// <param name="fieldMap">Field mapping configuration</param>
    /// <returns>Expression for use with IQueryable.Select()</returns>
    public static Expression<Func<TSource, Dictionary<string, object?>>> BuildDictionaryProjection<TSource>(
        string[]? fields,
        FieldMap<TSource> fieldMap)
    {
        var parameter = Expression.Parameter(typeof(TSource), "x");
        var dictType = typeof(Dictionary<string, object?>);
        var addMethod = dictType.GetMethod("Add", new[] { typeof(string), typeof(object) })!;

        List<ElementInit> bindings = new();

        // Get all available fields from fieldMap if no specific fields requested
        var fieldsToSelect = fields != null && fields.Length > 0
            ? fields
            : fieldMap.GetFieldNames().ToArray();

        foreach (var fieldName in fieldsToSelect)
        {
            if (!fieldMap.TryGet(fieldName, out var sourceExpression, out _))
            {
                continue;
            }

            if (!fieldMap.CanSelect(fieldName))
            {
                continue;
            }

            // Replace parameter in source expression
            ParameterReplacerVisitor visitor = new(sourceExpression.Parameters[0], parameter);
            var sourceBody = visitor.Visit(sourceExpression.Body);

            // Convert to object
            var objectValue = Expression.Convert(sourceBody, typeof(object));

            // Use camelCase for dictionary key
            var key = Expression.Constant(ToCamelCase(fieldName));
            bindings.Add(Expression.ElementInit(addMethod, key, objectValue));
        }

        var dictInit = Expression.ListInit(Expression.New(dictType), bindings);
        return Expression.Lambda<Func<TSource, Dictionary<string, object?>>>(dictInit, parameter);
    }

    /// <summary>
    /// Check if fields parameter has any values
    /// </summary>
    public static bool HasFieldSelection(string[]? fields)
    {
        return fields != null && fields.Length > 0;
    }

    /// <summary>
    /// Validate that all requested fields exist in the field map
    /// </summary>
    public static (bool IsValid, string[] InvalidFields) ValidateFields<TSource>(
        string[]? fields,
        FieldMap<TSource> fieldMap)
    {
        if (fields == null || fields.Length == 0)
        {
            return (true, Array.Empty<string>());
        }

        var invalidFields = fields
            .Where(f => !fieldMap.TryGet(f, out _, out _) || !fieldMap.CanSelect(f))
            .ToArray();

        return (invalidFields.Length == 0, invalidFields);
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
        {
            return str;
        }

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
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
