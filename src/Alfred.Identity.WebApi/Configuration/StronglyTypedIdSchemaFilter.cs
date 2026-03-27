using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Alfred.Identity.WebApi.Configuration;

/// <summary>
/// Swashbuckle schema filter that maps all strongly typed IDs from
/// Alfred.Identity.Domain.Common.Ids to a plain primitive schema instead of
/// rendering them as objects with a "Value" property.
/// </summary>
public sealed class StronglyTypedIdSchemaFilter : ISchemaFilter
{
    private static readonly Dictionary<Type, (string Type, string? Format)> PrimitiveSchemas = new()
    {
        [typeof(Guid)] = ("string", "uuid"),
        [typeof(long)] = ("integer", "int64"),
        [typeof(int)] = ("integer", "int32"),
        [typeof(string)] = ("string", null)
    };

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;

        if (!type.IsValueType || type.Namespace != "Alfred.Identity.Domain.Common.Ids")
        {
            return;
        }

        var valueType = type.GetProperty("Value")?.PropertyType;
        if (valueType is null || !PrimitiveSchemas.TryGetValue(valueType, out var mapping))
        {
            return;
        }

        schema.Type = mapping.Type;
        schema.Format = mapping.Format;
        schema.Properties.Clear();
        schema.AllOf.Clear();
    }
}
