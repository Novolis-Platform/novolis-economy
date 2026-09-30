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
