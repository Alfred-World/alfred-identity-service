using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alfred.Identity.WebApi.Configuration;

/// <summary>
/// JSON converter factory for <see cref="Optional{T}"/> that correctly handles
/// the "not sent" vs "sent as null" distinction in PATCH requests.
/// <para>
/// When a JSON key is missing, the property keeps its default value (HasValue = false).
/// When a JSON key is present (even with null), the converter sets HasValue = true.
/// </para>
/// </summary>
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType
               && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(innerType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

internal sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Optional<T>.Of(default!);
        }

        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return Optional<T>.Of(value!);
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
