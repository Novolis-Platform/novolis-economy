using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Stable identity of an economic model, independent of one run.</summary>
public readonly record struct EconomicModelIdentity(
    string Id,
    string Version)
{
    public override string ToString() => $"{Id}@{Version}";
}
