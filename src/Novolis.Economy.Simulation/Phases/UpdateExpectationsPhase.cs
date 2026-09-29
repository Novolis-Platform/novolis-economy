using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class UpdateExpectationsPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.UpdateExpectations;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    // Market book is already updated on trades; phase exists for future expectation models.
    _ = context.State.World.MarketBook;
    return ValueTask.CompletedTask;
  }
}
