using System.Text;
using System.Text.Json;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Registry of explicitly supported model snapshot codecs.</summary>
public sealed class EconomicModelCodecRegistry
{
    private readonly Dictionary<string, IEconomicModelCodec> _codecs =
        new(StringComparer.Ordinal);

    /// <summary>Registers one codec identity.</summary>
    public void Register(IEconomicModelCodec codec)
    {
        ArgumentNullException.ThrowIfNull(codec);
        var key = Key(codec.Identity);
        if (!_codecs.TryAdd(key, codec))
            throw new InvalidOperationException(
                $"A snapshot codec is already registered for '{key}'.");
    }

    /// <summary>Finds a codec or throws for an unsupported model version.</summary>
    public IEconomicModelCodec Resolve(EconomicModelIdentity identity)
    {
        if (_codecs.TryGetValue(Key(identity), out var codec))
            return codec;

        throw new InvalidDataException(
            $"No snapshot codec is registered for model '{identity}'.");
    }

    private static string Key(EconomicModelIdentity identity) =>
        $"{identity.Id}@{identity.Version}";
}

/// <summary>
/// Serializes, stores, and restores versioned economic model documents.
/// </summary>
public sealed class EconomicSnapshotStore
{
    private readonly EconomicModelCodecRegistry _registry;
    private readonly JsonSerializerOptions _options;

    /// <summary>Creates a store backed by an explicit codec registry.</summary>
    public EconomicSnapshotStore(
        EconomicModelCodecRegistry registry,
        bool indented = true)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _options = EconomicJson.CreateOptions(indented);
    }

    /// <summary>Captures a typed model state using its registered codec.</summary>
    public EconomicModelSnapshotDocument Capture(
        IEconomicModelCodec codec,
        IEconomicModel model,
        IEconomicScenario scenario,
        IEconomicModelState state,
        EconomicSnapshotCapture capture)
    {
        ArgumentNullException.ThrowIfNull(codec);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(capture.CommandStream);
        ArgumentNullException.ThrowIfNull(capture.Observations);
        ArgumentNullException.ThrowIfNull(capture.Transactions);
        if (capture.Tick < 0 ||
            capture.Period < 0 ||
            capture.Tick != state.Tick)
        {
            throw new ArgumentException(
                "Snapshot capture metadata does not match the model state.",
                nameof(capture));
        }

        foreach (var (tick, commands) in capture.CommandStream)
        {
            if (tick < 1 || tick > capture.Tick)
            {
                throw new ArgumentException(
                    $"Command stream tick {tick} is outside the captured run.",
                    nameof(capture));
            }

            if (commands is null || commands.Any(command => command is null))
            {
                throw new ArgumentException(
                    "The command stream cannot contain null commands.",
                    nameof(capture));
            }
        }

        if (codec.Identity != model.Identity)
            throw new InvalidOperationException(
                $"Codec '{codec.Identity}' cannot capture model '{model.Identity}'.");

        var document = codec.Capture(model, scenario, state, capture);
        EconomicJson.RequireCurrentSchema(document);
        ValidateEnvelope(document);
        return document;
    }

    /// <summary>Serializes a snapshot with canonical property ordering.</summary>
    public string Serialize(EconomicModelSnapshotDocument document)
    {
        EconomicJson.RequireCurrentSchema(document);
        return EconomicJson.SerializeCanonical(document, _options);
    }

    /// <summary>Writes a snapshot as UTF-8 JSON to a caller-owned stream.</summary>
    public void Write(
        Stream destination,
        EconomicModelSnapshotDocument document)
    {
        ArgumentNullException.ThrowIfNull(destination);
        var bytes = Encoding.UTF8.GetBytes(Serialize(document));
        destination.Write(bytes);
    }

    /// <summary>Reads and validates a snapshot document from UTF-8 JSON.</summary>
    public EconomicModelSnapshotDocument Read(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var document = JsonSerializer.Deserialize<EconomicModelSnapshotDocument>(
            source,
            _options);
        if (document is null)
            throw new InvalidDataException("The economic snapshot JSON was empty.");

        EconomicJson.RequireCurrentSchema(document);
        ValidateEnvelope(document);
        return document;
    }

    /// <summary>Reads, resolves, and restores a typed model state.</summary>
    public EconomicModelRestore Restore(Stream source)
    {
        var document = Read(source);
        return _registry.Resolve(document.Model).Restore(document);
    }

    /// <summary>Reads, resolves, and restores a typed model state from text.</summary>
    public EconomicModelRestore Restore(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var source = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return Restore(source);
    }

    private static void ValidateEnvelope(EconomicModelSnapshotDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Model.Id) ||
            string.IsNullOrWhiteSpace(document.Model.Version) ||
            string.IsNullOrWhiteSpace(document.ScenarioId) ||
            string.IsNullOrWhiteSpace(document.ScenarioVersion) ||
            string.IsNullOrWhiteSpace(document.SpecificationHash) ||
            string.IsNullOrWhiteSpace(document.CommandHash))
        {
            throw new InvalidDataException(
                "Economic snapshots require model and scenario identities.");
        }

        if (document.Tick < 0 || document.Period < 0)
        {
            throw new InvalidDataException(
                "Economic snapshot tick and period cannot be negative.");
        }

        if (document.Commands is null ||
            document.Observations is null ||
            document.Transactions is null)
        {
            throw new InvalidDataException(
                "Economic snapshot collections cannot be null.");
        }

        foreach (var command in document.Commands)
        {
            if (command.Tick < 1 || command.Tick > document.Tick ||
                string.IsNullOrWhiteSpace(command.Kind))
            {
                throw new InvalidDataException(
                    "Economic snapshot contains an invalid command envelope.");
            }
        }
    }
}
