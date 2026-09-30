using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Simulation.Bounded;

namespace Novolis.Economy.Simulation;

/// <summary>One ordered simulation phase.</summary>
public interface ISimulationPhase
{
  /// <summary>Phase order in the hourly pipeline.</summary>
  SimulationPhaseOrder Order { get; }

  /// <summary>Executes the phase for the current hour.</summary>
  ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken);
}
