using System.Collections;
using System.Reflection;

using Alfred.Identity.Application.Querying.Core;

namespace Alfred.Identity.Application.Querying.Extensions;

/// <summary>
/// Extensions for field selection/projection on DTOs
/// </summary>
public static class FieldSelectionExtensions
{
    /// <summary>
    /// Apply field selection to a list of DTOs
    /// Returns only the specified fields for each DTO
    /// Supports nested field selection: "userDevices.id,userDevices.deviceName"
    /// </summary>
    public static List<Dictionary<string, object?>> SelectFields<T>(
        this IEnumerable<T> items,
        string[]? fields) where T : class
    {
        List<Dictionary<string, object?>> result = new();

        if (fields == null || fields.Length == 0)
        {
            // No field selection - return all properties as dictionaries
            var allProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var item in items)
            {
                Dictionary<string, object?> dict = new(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in allProperties)
                {
                    dict[ToCamelCase(prop.Name)] = prop.GetValue(item);
                }

                result.Add(dict);
            }

            return result;
        }

        // Parse field selection into tree structure
        var fieldTree = ParseFieldTree(fields);

        foreach (var item in items)
        {
            var dict = ApplyFieldSelection(item, fieldTree);
            result.Add(dict);
        }

        return result;
    }

    /// <summary>
    /// Apply field selection to a PageResult
    /// </summary>
    public static PageResult<Dictionary<string, object?>> SelectFields<T>(
        this PageResult<T> pageResult,
        string[]? fields) where T : class
    {
        var selectedItems = pageResult.Items.SelectFields(fields);
        return new PageResult<Dictionary<string, object?>>(
            selectedItems,
            pageResult.Page,
            pageResult.PageSize,
            pageResult.Total);
    }

    /// <summary>
    /// Apply field selection to a PageResult and return standard response format
    /// Returns object with { data: [...], pagination: {...} } structure
    /// </summary>
    public static object SelectFieldsToResponse<T>(
        this PageResult<T> pageResult,
        string[]? fields) where T : class
    {
        var selectedItems = pageResult.Items.SelectFields(fields);
        return new
        {
            data = selectedItems,
            pagination = new
            {
                page = pageResult.Page,
                pageSize = pageResult.PageSize,
                total = pageResult.Total,
                totalPages = pageResult.TotalPages,
                hasPrevPage = pageResult.HasPrevPage,
                hasNextPage = pageResult.HasNextPage
            }
        };
    }

    /// <summary>
    /// Parse field paths into tree structure for nested selection
    /// Example: ["id", "name", "userDevices.id", "userDevices.deviceName"]
    /// -> { "id": null, "name": null, "userDevices": { "id": null, "deviceName": null } }
    /// </summary>
    private static Dictionary<string, Dictionary<string, Dictionary<string, object?>?>?> ParseFieldTree(string[] fields)
    {
        Dictionary<string, Dictionary<string, Dictionary<string, object?>?>?> tree =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            var parts = field.Split('.', 2);
            var rootField = parts[0];

            if (parts.Length == 1)
            {
                // Simple field
                if (!tree.ContainsKey(rootField))
                {
                    tree[rootField] = null;
                }
            }
            else
            {
                // Nested field
                if (!tree.TryGetValue(rootField, out var nestedFields))
                {
                    nestedFields =
                        new Dictionary<string, Dictionary<string, object?>?>(StringComparer.OrdinalIgnoreCase);
                    tree[rootField] = nestedFields;
                }

                if (nestedFields != null)
                {
                    var nestedField = parts[1];
                    if (!nestedFields.ContainsKey(nestedField))
                    {
                        nestedFields[nestedField] = null;
                    }
                }
            }
        }

        return tree;
    }

    /// <summary>
    /// Apply field selection recursively to an object
    /// </summary>
    private static Dictionary<string, object?> ApplyFieldSelection(
        object? obj,
        Dictionary<string, Dictionary<string, Dictionary<string, object?>?>?> fieldTree)
    {
        Dictionary<string, object?> result = new(StringComparer.OrdinalIgnoreCase);

        if (obj == null)
        {
            return result;
        }

        var type = obj.GetType();

        foreach (var kvp in fieldTree)
        {
            var fieldName = kvp.Key;
            var nestedFields = kvp.Value;

            var prop = type.GetProperty(fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null)
            {
                continue;
            }

            var value = prop.GetValue(obj);

            if (nestedFields == null)
            {
                // Simple field - include as is
                result[ToCamelCase(prop.Name)] = value;
            }
            else
            {
                // Nested field - apply selection recursively
                if (value == null)
                {
                    result[ToCamelCase(prop.Name)] = null;
                }
                else if (value is IEnumerable enumerable && !(value is string))
                {
                    // Collection - apply to each item
                    List<Dictionary<string, object?>> selectedItems = new();
                    foreach (var item in enumerable)
                    {
                        if (item != null)
                        {
                            var selectedItem = ApplyNestedFieldSelection(item, nestedFields);
                            selectedItems.Add(selectedItem);
                        }
                    }

                    result[ToCamelCase(prop.Name)] = selectedItems;
                }
                else
                {
                    // Single nested object
                    var selectedNested = ApplyNestedFieldSelection(value, nestedFields);
                    result[ToCamelCase(prop.Name)] = selectedNested;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Apply field selection to nested object (one level deep)
    /// </summary>
    private static Dictionary<string, object?> ApplyNestedFieldSelection(
        object obj,
        Dictionary<string, Dictionary<string, object?>?> nestedFields)
    {
        Dictionary<string, object?> result = new(StringComparer.OrdinalIgnoreCase);
        var type = obj.GetType();

        foreach (var fieldName in nestedFields.Keys)
        {
            var prop = type.GetProperty(fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                result[ToCamelCase(prop.Name)] = prop.GetValue(obj);
            }
        }

        return result;
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
        {
            return str;
        }

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
