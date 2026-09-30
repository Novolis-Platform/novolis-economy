using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Context for one host-driven model tick.</summary>
public sealed record EconomicTickContext(
    long Tick,
    int Period,
    bool IsPeriodBoundary,
    IReadOnlyList<IEconomicModelCommand> Commands,
    IAgentRandom? Random = null,
    ulong Seed = 0);
