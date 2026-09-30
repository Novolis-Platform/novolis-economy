using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

/// <summary>
/// Serializes the Guid-backed identifier records used throughout the economic
/// domain as canonical strings, including dictionary keys.
/// </summary>
internal sealed class GuidValueObjectConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsValueType ||
            typeToConvert == typeof(Guid) ||
            typeToConvert.GetProperty("Value")?.PropertyType != typeof(Guid))
        {
            return false;
        }

        return typeToConvert.GetConstructor([typeof(Guid)]) is not null;
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var converterType = typeof(GuidValueObjectConverter<>)
            .MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(
            converterType,
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: null,
            culture: null)!;
    }
}
