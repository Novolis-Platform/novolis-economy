using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

/// <summary>
/// Canonical JSON options shared by model codecs and the Simulation store.
/// </summary>
public static class EconomicJson
{
    /// <summary>Creates the stable serializer options for persisted documents.</summary>
    public static JsonSerializerOptions CreateOptions(bool indented = false)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = indented
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new GuidValueObjectConverterFactory());
        return options;
    }

    /// <summary>Serializes a concrete value into a detached JSON element.</summary>
    public static JsonElement ToElement<T>(T value, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.SerializeToElement(value, value.GetType(), options ?? CreateOptions());
    }

    /// <summary>Serializes a value with recursively sorted object properties.</summary>
    public static string SerializeCanonical<T>(
        T value,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(
                value,
                value.GetType(),
                options ?? CreateOptions()));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteCanonical(document.RootElement, writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Hashes the canonical representation of a persisted value.</summary>
    public static string HashCanonical<T>(
        T value,
        JsonSerializerOptions? options = null)
    {
        var json = SerializeCanonical(value, options);
        return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(json)))
            .ToLowerInvariant();
    }

    /// <summary>Throws when a document does not use the supported schema.</summary>
    public static void RequireCurrentSchema(EconomicModelSnapshotDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!string.Equals(
                document.FormatVersion,
                EconomicPersistenceSchema.CurrentFormatVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Unsupported economic snapshot format '{document.FormatVersion}'.");
        }
    }

    private static void WriteCanonical(
        JsonElement element,
        Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject()
                             .OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteCanonical(item, writer);

                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
