using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Machine-readable validation result for one seeded model run.
/// Internal invariants and empirical targets remain visibly distinct.
/// </summary>
public sealed record EconomicValidationReport(
    EconomicModelIdentity Model,
    string ScenarioId,
    string ScenarioVersion,
    ulong Seed,
    long Ticks,
    string SpecificationHash,
    IReadOnlyList<EconomicValidationSignal> Invariants,
    IReadOnlyList<EconomicValidationMeasurement> Measurements)
{
    /// <summary>Whether every invariant and declared target passed.</summary>
    public bool Passed =>
        Invariants.All(signal => signal.Passed) &&
        Measurements.All(measurement => measurement.Passed);
}
