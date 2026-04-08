using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alfred.Identity.Domain.Querying;

/// <summary>
/// Custom JSON converter for deserializing HotChocolate-inspired filter DSL into FilterNode tree.
///
/// Supported JSON shapes:
/// - Logical:    { "and": [...] } or { "or": [...] }
/// - Field:      { "email": { "contains": "admin" } }
/// - Collection: { "roles": { "some": { "name": { "eq": "Admin" } } } }
/// - Implicit AND when multiple keys at same level: { "email": { "eq": "a" }, "name": { "eq": "b" } }
/// </summary>
public sealed class FilterNodeJsonConverter : JsonConverter<FilterNode>
{
    private const int MaxDepth = 10;

    public override FilterNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Filter must be a JSON object");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        return ParseNode(doc.RootElement, 0);
    }

    public override void Write(Utf8JsonWriter writer, FilterNode value, JsonSerializerOptions options)
    {
        WriteNode(writer, value);
    }

    private FilterNode ParseNode(JsonElement element, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new JsonException($"Filter nesting exceeds maximum depth of {MaxDepth}");
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Each filter node must be a JSON object");
        }

        var nodes = new List<FilterNode>();

        foreach (var prop in element.EnumerateObject())
        {
            var key = prop.Name;

            if (string.Equals(key, "and", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "or", StringComparison.OrdinalIgnoreCase))
            {
                var op = string.Equals(key, "and", StringComparison.OrdinalIgnoreCase)
                    ? LogicalOperator.And
                    : LogicalOperator.Or;

                if (prop.Value.ValueKind != JsonValueKind.Array)
                {
                    throw new JsonException($"'{key}' must be an array of filter objects");
                }

                var conditions = new List<FilterNode>();
                foreach (var item in prop.Value.EnumerateArray())
                {
                    conditions.Add(ParseNode(item, depth + 1));
                }

                nodes.Add(new LogicalFilterNode(op, conditions));
            }
            else
            {
                // Field name — value is an object with operators
                if (prop.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException($"Field '{key}' value must be a JSON object with operators");
                }

                nodes.Add(ParseFieldOrCollection(key, prop.Value, depth));
            }
        }

        return nodes.Count == 1 ? nodes[0] : new LogicalFilterNode(LogicalOperator.And, nodes);
    }

    private FilterNode ParseFieldOrCollection(string fieldName, JsonElement value, int depth)
    {
        var fieldOps = new List<FieldOperation>();
        FilterNode? collectionResult = null;

        foreach (var prop in value.EnumerateObject())
        {
            var opName = prop.Name;

            if (CollectionOperators.IsCollectionOperator(opName))
            {
                var colOp = CollectionOperators.Parse(opName);

                if (colOp == CollectionOperator.Any)
                {
                    // "any" takes a boolean value (check if collection is empty or not)
                    collectionResult = new CollectionFilterNode(fieldName, colOp, null);
                }
                else
                {
                    // "some", "all", "none" take an inner filter object
                    if (prop.Value.ValueKind != JsonValueKind.Object)
                    {
                        throw new JsonException(
                            $"Collection operator '{opName}' on field '{fieldName}' must have an object value");
                    }

                    var innerFilter = ParseNode(prop.Value, depth + 1);
                    collectionResult = new CollectionFilterNode(fieldName, colOp, innerFilter);
                }
            }
            else
            {
                // Comparison operator
                if (!ComparisonOperators.IsComparisonOperator(opName))
                {
                    throw new JsonException(
                        $"Unknown operator '{opName}' on field '{fieldName}'. " +
                        $"Valid operators: {string.Join(", ", ComparisonOperators.All)}");
                }

                fieldOps.Add(new FieldOperation(opName, ExtractValue(prop.Value)));
            }
        }

        // If we found a collection operator, return that (collection operators don't mix with comparison)
        if (collectionResult != null)
        {
            return collectionResult;
        }

        if (fieldOps.Count == 0)
        {
            throw new JsonException($"Field '{fieldName}' has no operators");
        }

        return new FieldFilterNode(fieldName, fieldOps);
    }

    private static object? ExtractValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ExtractValue).ToList(),
            _ => throw new JsonException($"Unsupported filter value type: {element.ValueKind}")
        };
    }

    private static void WriteNode(Utf8JsonWriter writer, FilterNode node)
    {
        switch (node)
        {
            case LogicalFilterNode logical:
                writer.WriteStartObject();
                writer.WritePropertyName(logical.Operator == LogicalOperator.And ? "and" : "or");
                writer.WriteStartArray();
                foreach (var condition in logical.Conditions)
                {
                    WriteNode(writer, condition);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                break;

            case FieldFilterNode field:
                writer.WriteStartObject();
                writer.WritePropertyName(field.FieldName);
                writer.WriteStartObject();
                foreach (var op in field.Operations)
                {
                    writer.WritePropertyName(op.Operator);
                    JsonSerializer.Serialize(writer, op.Value);
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
                break;

            case CollectionFilterNode collection:
                writer.WriteStartObject();
                writer.WritePropertyName(collection.FieldName);
                writer.WriteStartObject();
                var opName = collection.Operator switch
                {
                    CollectionOperator.Some => "some",
                    CollectionOperator.All => "all",
                    CollectionOperator.None => "none",
                    CollectionOperator.Any => "any",
                    _ => throw new NotSupportedException()
                };
                writer.WritePropertyName(opName);
                if (collection.Operator == CollectionOperator.Any)
                {
                    writer.WriteBooleanValue(true);
                }
                else if (collection.InnerFilter != null)
                {
                    WriteNode(writer, collection.InnerFilter);
                }
                else
                {
                    writer.WriteNullValue();
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
                break;
        }
    }
}
