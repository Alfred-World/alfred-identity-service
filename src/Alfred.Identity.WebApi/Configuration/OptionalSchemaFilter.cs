using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Alfred.Identity.WebApi.Configuration;

/// <summary>
/// Swagger schema filter that unwraps <see cref="Optional{T}"/> to its inner type
/// so API documentation shows the actual type instead of an object with HasValue/Value properties.
/// All Optional fields are rendered as nullable (not required) in the schema.
/// </summary>
public sealed class OptionalSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsGenericType || context.Type.GetGenericTypeDefinition() != typeof(Optional<>))
        {
            return;
        }

        var innerType = context.Type.GetGenericArguments()[0];

        // Unwrap Nullable<T> struct (e.g. Optional<Guid?>, Optional<AccountProductType?>)
        var baseType = Nullable.GetUnderlyingType(innerType) ?? innerType;
        var isNullableValueType = Nullable.GetUnderlyingType(innerType) != null;

        var innerSchema = context.SchemaGenerator.GenerateSchema(baseType, context.SchemaRepository);

        schema.Nullable = isNullableValueType || innerSchema.Nullable;
        schema.Reference = null;

        if (innerSchema.Reference != null)
        {
            var refId = innerSchema.Reference.Id;

            if (refId.EndsWith("Optional", StringComparison.Ordinal)
                && context.SchemaRepository.Schemas.TryGetValue(refId, out var refSchema))
            {
                // *Optional wrapper (will be removed by document filter) — resolve inline
                schema.Type = refSchema.Type;
                schema.Format = refSchema.Format;
                schema.Enum = refSchema.Enum;
                schema.Properties = refSchema.Properties;
                schema.Items = refSchema.Items;
                schema.AllOf = refSchema.AllOf;
            }
            else
            {
                // Named schema (enum, complex type) — keep $ref via allOf to avoid duplicate names
                schema.AllOf = [new OpenApiSchema { Reference = innerSchema.Reference }];
            }
        }
        else
        {
            // Primitive / inline type — copy directly
            schema.Type = innerSchema.Type;
            schema.Format = innerSchema.Format;
            schema.Enum = innerSchema.Enum;
            schema.Properties = innerSchema.Properties;
            schema.Items = innerSchema.Items;
            schema.AllOf = innerSchema.AllOf;
        }
    }
}

/// <summary>
/// Removes orphan Optional-derived schemas (e.g. StringOptional, BooleanOptional)
/// from the OpenAPI components/schemas section after generation.
/// </summary>
public sealed class OptionalSchemaDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var keysToRemove = swaggerDoc.Components.Schemas.Keys
            .Where(k => k.EndsWith("Optional", StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            swaggerDoc.Components.Schemas.Remove(key);
        }
    }
}
