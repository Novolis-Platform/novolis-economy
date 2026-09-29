using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class ApplyResearchProgressPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.ApplyResearchProgress;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    foreach (var (firmId, budget) in world.ResearchBudget.OrderBy(kv => kv.Key.Value))
    {
      if (budget.Amount <= 0m)
      {
        continue;
      }

      var gain = budget.Amount * world.Policy.ResearchProductivityPerCurrency;
      world.Productivity[firmId] = Math.Min(2m, world.Productivity.GetValueOrDefault(firmId, 1m) + gain);
      world.ResearchBudget[firmId] = Money.Zero;
    }

    return ValueTask.CompletedTask;
  }
}
