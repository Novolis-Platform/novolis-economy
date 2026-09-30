using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Read-side receipt for an authoritative economic transition.</summary>
public sealed record EconomicTransitionReceipt(
    TransactionId TransactionId,
    string Reason,
    int Period,
    long Tick,
    IReadOnlyList<EconomicEffectRequest> Effects);
