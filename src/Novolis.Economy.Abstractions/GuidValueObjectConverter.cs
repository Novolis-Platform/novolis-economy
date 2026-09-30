using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

internal sealed class GuidValueObjectConverter<T> : JsonConverter<T>
    where T : struct
{
    private static readonly PropertyInfo ValueProperty =
        typeof(T).GetProperty("Value")
        ?? throw new InvalidOperationException(
            $"'{typeof(T).Name}' does not expose a Guid Value property.");

    public override T Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (!Guid.TryParse(text, out var value))
            throw new JsonException(
                $"Expected a Guid-backed identifier string for {typeof(T).Name}.");

        return Create(value);
    }

    public override void Write(
        Utf8JsonWriter writer,
        T value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(ReadGuid(value).ToString("N"));

    public override T ReadAsPropertyName(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (!Guid.TryParse(text, out var value))
            throw new JsonException(
                $"Expected a Guid-backed identifier property for {typeof(T).Name}.");

        return Create(value);
    }

    public override void WriteAsPropertyName(
        Utf8JsonWriter writer,
        T value,
        JsonSerializerOptions options) =>
        writer.WritePropertyName(ReadGuid(value).ToString("N"));

    private static Guid ReadGuid(T value) =>
        (Guid)(ValueProperty.GetValue(value)
            ?? throw new JsonException(
                $"Identifier {typeof(T).Name} has no Guid value."));

    private static T Create(Guid value) =>
        (T)(Activator.CreateInstance(typeof(T), value)
            ?? throw new JsonException(
                $"Could not create identifier {typeof(T).Name}."));
}
