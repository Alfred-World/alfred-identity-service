using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alfred.Identity.WebApi.Configuration;

/// <summary>
/// A <see cref="JsonConverterFactory"/> that handles all strongly typed IDs
/// from Alfred.Identity.Domain.Common.Ids, serializing them as their underlying
/// primitive type: Guid → UUID string, long/int → number, string → string.
/// </summary>
public sealed class StronglyTypedIdJsonConverterFactory : JsonConverterFactory
{
    private static readonly HashSet<Type> SupportedValueTypes =
    [
        typeof(Guid), typeof(long), typeof(int), typeof(string)
    ];

    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsValueType || typeToConvert.Namespace != "Alfred.Identity.Domain.Common.Ids")
        {
            return false;
        }

        var valueType = typeToConvert.GetProperty("Value")?.PropertyType;
        return valueType is not null && SupportedValueTypes.Contains(valueType);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetProperty("Value")!.PropertyType;
        var converterType = typeof(StronglyTypedIdJsonConverter<,>).MakeGenericType(typeToConvert, valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class StronglyTypedIdJsonConverter<TId, TValue> : JsonConverter<TId>
        where TId : struct
    {
        private static readonly PropertyInfo ValueProperty = typeof(TId).GetProperty("Value")!;
        private static readonly ConstructorInfo Constructor = typeof(TId).GetConstructor([typeof(TValue)])!;

        private static readonly Action<Utf8JsonWriter, TValue> WriteValue = ResolveWriter();

        private static Action<Utf8JsonWriter, TValue> ResolveWriter()
        {
            if (typeof(TValue) == typeof(Guid))
            {
                return (w, v) => w.WriteStringValue((Guid)(object)v!);
            }

            if (typeof(TValue) == typeof(long))
            {
                return (w, v) => w.WriteNumberValue((long)(object)v!);
            }

            if (typeof(TValue) == typeof(int))
            {
                return (w, v) => w.WriteNumberValue((int)(object)v!);
            }

            if (typeof(TValue) == typeof(string))
            {
                return (w, v) => w.WriteStringValue((string)(object)v!);
            }

            throw new InvalidOperationException($"Unsupported strongly typed ID value type: {typeof(TValue)}");
        }

        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            object rawValue;
            if (typeof(TValue) == typeof(Guid))
            {
                rawValue = reader.GetGuid();
            }
            else if (typeof(TValue) == typeof(long))
            {
                rawValue = reader.GetInt64();
            }
            else if (typeof(TValue) == typeof(int))
            {
                rawValue = reader.GetInt32();
            }
            else if (typeof(TValue) == typeof(string))
            {
                rawValue = reader.GetString() ?? string.Empty;
            }
            else
            {
                throw new JsonException($"Unsupported strongly typed ID value type: {typeof(TValue)}");
            }

            return (TId)Constructor.Invoke([(TValue)rawValue]);
        }

        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            WriteValue(writer, (TValue)ValueProperty.GetValue(value)!);
        }
    }
}
