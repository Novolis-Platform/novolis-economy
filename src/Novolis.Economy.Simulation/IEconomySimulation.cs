using Novolis.Economy;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Agents;

namespace Novolis.Economy.Simulation;

/// <summary>Headless economic simulation entry point.</summary>
public interface IEconomySimulation
{
  /// <summary>Current mutable state.</summary>
  SimulationState State { get; }

  /// <summary>Enqueues a command for a future tick.</summary>
  void Enqueue(IEconomyCommand command);

  /// <summary>Advances the simulation by the given duration.</summary>
  ValueTask<SimulationResult> AdvanceAsync(
    SimulationDuration duration,
    CancellationToken cancellationToken = default);
}
