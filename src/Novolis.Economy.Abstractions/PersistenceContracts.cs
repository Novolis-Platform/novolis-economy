using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

/// <summary>Version marker for persisted economic model documents.</summary>
public static class EconomicPersistenceSchema
{
    /// <summary>First stable JSON snapshot schema.</summary>
    public const string CurrentFormatVersion = "economy.snapshot.v1";
}

/// <summary>
/// A command preserved in a snapshot without relying on CLR type names.
/// </summary>
public sealed record EconomicCommandDocument(
    long Tick,
    string Kind,
    JsonElement Payload);

/// <summary>
/// Run material captured alongside a model state snapshot.
/// </summary>
public sealed record EconomicSnapshotCapture(
    ulong Seed,
    long Tick,
    int Period,
    IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>> CommandStream,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions);

/// <summary>
/// Versioned, model-neutral persisted representation of one economic state.
/// Model-specific codecs own the contents of the JSON payloads.
/// </summary>
public sealed record EconomicModelSnapshotDocument(
    string FormatVersion,
    EconomicModelIdentity Model,
    string ScenarioId,
    string ScenarioVersion,
    ulong Seed,
    long Tick,
    int Period,
    ulong StateFingerprint,
    JsonElement Specification,
    JsonElement Scenario,
    JsonElement State,
    IReadOnlyList<EconomicCommandDocument> Commands,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions);

/// <summary>Restored typed model and state returned by a model codec.</summary>
public sealed record EconomicModelRestore(
    IEconomicModel Model,
    IEconomicScenario Scenario,
    IEconomicModelState State,
    IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>> CommandStream,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions);

/// <summary>
/// Converts one concrete model to and from the stable persistence document.
/// Implementations must identify commands by domain kind, never by CLR name.
/// </summary>
public interface IEconomicModelCodec
{
    /// <summary>Model identity accepted by this codec.</summary>
    EconomicModelIdentity Identity { get; }

    /// <summary>Captures a typed model state into a data-only document.</summary>
    EconomicModelSnapshotDocument Capture(
        IEconomicModel model,
        IEconomicScenario scenario,
        IEconomicModelState state,
        EconomicSnapshotCapture capture);

    /// <summary>Restores and validates a typed model state from a document.</summary>
    EconomicModelRestore Restore(EconomicModelSnapshotDocument document);
}

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
