using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class ResolveConsumerPurchasesPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.ResolveConsumerPurchases;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var inventoryBefore = CoreInventoryBridge.Snapshot(world);
    DemandEngine.ResolvePurchases(
      world.Cohorts,
      world.RetailPrices,
      world.RetailFacilityMap(),
      world.Products,
      world.Inventory,
      world.Ledgers,
      context.State.Clock,
      e =>
      {
        context.State.AppendEvent(e);
        if (e is MarketTradeObserved trade)
        {
          world.MarketBook.RecordTrade(trade.ProductId, trade.Quantity, trade.UnitPrice, trade.Hour);
        }
      },
      world.Policy.PriceElasticity);

    CoreInventoryBridge.ReconcileChanges(world, inventoryBefore);
    return ValueTask.CompletedTask;
  }
}
