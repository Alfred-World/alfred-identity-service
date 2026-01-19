using Alfred.Identity.Application.Querying.Validation;

namespace Alfred.Identity.Application.Common.Exceptions;

/// <summary>
/// Helper class for creating user-friendly filter exception messages
/// </summary>
public static class FilterExceptionHelper
{
    /// <summary>
    /// Creates a detailed filter exception with available fields and examples
    /// </summary>
    public static InvalidOperationException CreateFilterException<T>(
        Exception innerException,
        string filterSyntax,
        FieldMap<T> fieldMap) where T : class
    {
        var fieldDescriptions = GetFieldDescriptions(fieldMap);
        var examples = GetExamples(fieldMap);

        var message = $"Filter error: {innerException.Message}\n\n" +
                      $"Filter syntax: {filterSyntax}\n\n" +
                      $"Available fields:\n{fieldDescriptions}\n\n" +
                      $"Examples:\n{examples}";

        return new InvalidOperationException(message, innerException);
    }

    private static string GetFieldDescriptions<T>(FieldMap<T> fieldMap) where T : class
    {
        List<string> descriptions = new();

        foreach (var field in fieldMap.GetAllFields())
        {
            var fieldName = field.FieldName;
            var expression = field.Expression;

            // Determine field type from expression
            var returnType = expression.ReturnType;
            var fieldType = GetFieldTypeName(returnType);
            var operations = GetOperationsForType(returnType);

            descriptions.Add($"  - {fieldName} ({fieldType}): Use {operations}");
        }

        return string.Join("\n", descriptions);
    }

    private static string GetFieldTypeName(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
        {
            return "string";
        }

        if (underlyingType == typeof(bool))
        {
            return "boolean";
        }

        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(decimal) || underlyingType == typeof(double) ||
            underlyingType == typeof(float))
        {
            return "number";
        }

        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
        {
            return "datetime";
        }

        if (underlyingType == typeof(Guid))
        {
            return "guid";
        }

        // Handle object type (nullable fields)
        if (type == typeof(object))
        {
            return "string"; // Most object fields are nullable strings
        }

        return "unknown";
    }

    private static string GetOperationsForType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string) || type == typeof(object))
        {
            return "@contains, @startswith, @endswith, ==";
        }

        if (underlyingType == typeof(bool))
        {
            return "== true or == false";
        }

        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(decimal) || underlyingType == typeof(double) ||
            underlyingType == typeof(float) || underlyingType == typeof(DateTime) ||
            underlyingType == typeof(DateTimeOffset))
        {
            return "==, !=, >, <, >=, <=";
        }

        if (underlyingType == typeof(Guid))
        {
            return "==, !=";
        }

        return "==, !=";
    }

    private static string GetExamples<T>(FieldMap<T> fieldMap) where T : class
    {
        List<string> examples = new();
        var fields = fieldMap.GetAllFields().ToList();

        // Try to generate smart examples based on available fields
        foreach (var field in fields.Take(3)) // Take first 3 fields for examples
        {
            var fieldName = field.FieldName;
            var returnType = field.Expression.ReturnType;
            var underlyingType = Nullable.GetUnderlyingType(returnType) ?? returnType;

            if (underlyingType == typeof(string) || returnType == typeof(object))
            {
                if (fieldName.ToLower().Contains("name"))
                {
                    examples.Add($"  - {fieldName} @contains 'text'");
                }
                else if (fieldName.ToLower().Contains("email"))
                {
                    examples.Add($"  - {fieldName} @contains '@example.com'");
                }
                else
                {
                    examples.Add($"  - {fieldName} @startswith 'prefix'");
                }
            }
            else if (underlyingType == typeof(bool))
            {
                examples.Add($"  - {fieldName} == true");
            }
            else if (underlyingType == typeof(int) || underlyingType == typeof(long))
            {
                examples.Add($"  - {fieldName} > 0");
            }
            else if (underlyingType == typeof(DateTime))
            {
                examples.Add($"  - {fieldName} >= '2024-01-01'");
            }
        }

        // Add a combined example if we have multiple fields
        if (examples.Count >= 2)
        {
            var firstField = fields[0].FieldName;
            var secondField = fields[1].FieldName;
            var firstType = Nullable.GetUnderlyingType(fields[0].Expression.ReturnType) ??
                            fields[0].Expression.ReturnType;
            var secondType = Nullable.GetUnderlyingType(fields[1].Expression.ReturnType) ??
                             fields[1].Expression.ReturnType;

            if (firstType == typeof(bool) &&
                (secondType == typeof(string) || fields[1].Expression.ReturnType == typeof(object)))
            {
                examples.Add($"  - {firstField} == true and {secondField} @contains 'text'");
            }
        }

        return examples.Any() ? string.Join("\n", examples) : "  - No examples available";
    }
}
