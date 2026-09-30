using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Request for one seeded validation run.</summary>
public sealed record EconomicValidationRequest(
    IEconomicModel Model,
    IEconomicScenario Scenario,
    ulong Seed,
    long Ticks,
    CalibrationPlan Plan,
    int PeriodLengthTicks = 24,
    IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>>? Commands = null);
