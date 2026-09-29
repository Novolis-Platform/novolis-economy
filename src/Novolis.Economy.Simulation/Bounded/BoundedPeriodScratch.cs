using Novolis.Economy.Core;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>
/// Ephemeral data produced while executing the deterministic bounded profile.
/// It is owned by Simulation and is never an economic stock of record.
/// </summary>
public sealed record BoundedPeriodScratch(
  IReadOnlyDictionary<RegionId, decimal> LaborSupplyByRegion,
  IReadOnlyDictionary<ActivityId, decimal> LaborAllocated,
  IReadOnlyDictionary<ActivityId, decimal> ActualRuns,
  int HouseholdsMigrated = 0)
{
  /// <summary>Empty scratch for the beginning of a bounded period.</summary>
  public static BoundedPeriodScratch Empty { get; } = new(
    new Dictionary<RegionId, decimal>(),
    new Dictionary<ActivityId, decimal>(),
    new Dictionary<ActivityId, decimal>());

  /// <summary>Gets scratch from a bounded run state.</summary>
  public static BoundedPeriodScratch Get(BoundedPeriodState state) =>
    state.Scratch;

  /// <summary>Replaces scratch without changing authoritative Core state.</summary>
  public static BoundedPeriodState Attach(BoundedPeriodState state, BoundedPeriodScratch scratch) =>
    state with { Scratch = scratch };
}
