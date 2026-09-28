using Novolis.Economy;

namespace Novolis.Economy.Simulation;

/// <summary>Per-tick context passed to phases.</summary>
/// <param name="state">Mutable simulation state.</param>
/// <param name="random">Seeded RNG.</param>
/// <param name="entropy">Named deterministic entropy streams.</param>
public sealed class SimulationContext(
  SimulationState state,
  IEconomyRandom random,
  SimulationEntropy? entropy = null)
{
  /// <summary>Shared mutable state.</summary>
  public SimulationState State { get; } = state;

  /// <summary>Deterministic random source.</summary>
  public IEconomyRandom Random { get; } = random;

  /// <summary>Named deterministic streams derived from the run seed.</summary>
  public SimulationEntropy Entropy { get; } = entropy ?? state.Entropy;

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
