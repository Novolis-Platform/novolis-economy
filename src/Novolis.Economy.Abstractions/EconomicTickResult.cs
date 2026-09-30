using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Result of one host-driven model transition.</summary>
public sealed record EconomicTickResult(
    IEconomicModelState State,
    IReadOnlyList<EconomicTransitionReceipt> Transactions,
    IReadOnlyList<EconomicObservation> Observations);
