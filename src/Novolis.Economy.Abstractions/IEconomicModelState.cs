using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Opaque state owned by a concrete economic model.</summary>
public interface IEconomicModelState
{
    EconomicModelIdentity Model { get; }

    long Tick { get; }

    ulong Fingerprint { get; }
}
