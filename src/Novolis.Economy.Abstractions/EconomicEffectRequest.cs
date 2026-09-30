using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>One model-owned transition request for the authoritative kernel.</summary>
public sealed record EconomicEffectRequest(
    EconomicEntityId? Actor,
    string Kind,
    EconomicEntityId? Owner = null,
    EconomicAssetId? Asset = null,
    decimal Quantity = 0m,
    RegionId? Region = null,
    string? Reason = null);
