using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Simulation.Bounded;

namespace Novolis.Economy.Simulation;

/// <summary>Per-tick context passed to phases.</summary>
/// <param name="state">Mutable simulation state.</param>
/// <param name="random">Seeded RNG.</param>
/// <param name="entropy">Named deterministic entropy streams.</param>
/// <param name="periodEngine">Simulation-owned bounded period runner.</param>
/// <param name="agents">Rules-based actors scheduled before decisions apply.</param>
/// <param name="simulationHost">Optional product host exposed to compatibility actors.</param>
public sealed class SimulationContext(
  SimulationState state,
  IEconomyRandom random,
  SimulationEntropy? entropy = null,
  BoundedPeriodEngine? periodEngine = null,
  IReadOnlyList<IEconomicAgent>? agents = null,
  object? simulationHost = null)
{
  /// <summary>Shared mutable state.</summary>
  public SimulationState State { get; } = state;

  /// <summary>Deterministic random source.</summary>
  public IEconomyRandom Random { get; } = random;

  /// <summary>Named deterministic streams derived from the run seed.</summary>
  public SimulationEntropy Entropy { get; } = entropy ?? state.Entropy;

  /// <summary>Selected Simulation-owned period runner.</summary>
  public BoundedPeriodEngine PeriodEngine { get; } =
    periodEngine ?? DefaultBoundedPeriodPipeline.CreateEngine();

  /// <summary>Selected actors; Simulation schedules them, Core commits effects.</summary>
  public IReadOnlyList<IEconomicAgent> Agents { get; } =
    agents ?? Array.Empty<IEconomicAgent>();

  /// <summary>Optional product host supplied to compatibility actors.</summary>
  public object? SimulationHost { get; } = simulationHost;

  /// <summary>
  /// When true, non-essential hourly phases are skipped (decision apply + transport + hub match only).
  /// Used for UI max-speed free-run; clear to resume the full economy tick.
  /// </summary>
  public bool ThroughputMode { get; set; }
}

/// <summary>One ordered simulation phase.</summary>
public interface ISimulationPhase
{
  /// <summary>Phase order in the hourly pipeline.</summary>
  SimulationPhaseOrder Order { get; }

  /// <summary>Executes the phase for the current hour.</summary>
  ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken);
}

/// <summary>Diagnostic event emitted by skeleton phases.</summary>
/// <param name="Hour">Hour when the phase ran.</param>
/// <param name="Phase">Phase that executed.</param>
public sealed record PhaseExecuted(SimulationHour Hour, SimulationPhaseOrder Phase) : IEconomyEvent;
