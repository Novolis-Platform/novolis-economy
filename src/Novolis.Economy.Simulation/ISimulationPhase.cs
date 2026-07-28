using Novolis.Economy;

namespace Novolis.Economy.Simulation;

/// <summary>Per-tick context passed to phases.</summary>
/// <param name="state">Mutable simulation state.</param>
/// <param name="random">Seeded RNG.</param>
public sealed class SimulationContext(SimulationState state, IEconomyRandom random)
{
  /// <summary>Shared mutable state.</summary>
  public SimulationState State { get; } = state;

  /// <summary>Deterministic random source.</summary>
  public IEconomyRandom Random { get; } = random;
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
