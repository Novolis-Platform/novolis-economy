using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class RunProductionPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.RunProduction;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;
    var inventoryBefore = CoreInventoryBridge.Snapshot(world);

    if (world.Policy.EnableSpoilage)
    {
      foreach (var (key, qty, cost) in ProductionEngine.ApplySpoilage(world.Inventory, world.Products, hour))
      {
        if (world.Ledgers.TryGetValue(key.FirmId, out var ledger) && cost.Amount > 0m)
        {
          LedgerEngine.WriteOffInventory(ledger, cost, hour.Date);
        }

        context.State.AppendEvent(new InventorySpoiled(hour, key.FirmId, key.ProductId, qty));
      }
    }

    foreach (var plan in world.ProductionPlans.OrderBy(p => p.Key.Firm.Value).ThenBy(p => p.Key.Product.Value))
    {
      if (!world.Facilities.TryGetValue(plan.Key.Facility, out var facility) ||
          !world.Products.TryGetValue(plan.Key.Product, out var product))
      {
        continue;
      }

      var labor = world.AllocatedLaborHours.GetValueOrDefault(plan.Key.Firm);
      var productivity = world.Productivity.GetValueOrDefault(plan.Key.Firm, 1m);
      var produced = ProductionEngine.TryProduce(
        product,
        world.Inventory,
        plan.Key.Firm,
        facility.StorageLocation,
        plan.Value,
        facility.ManufacturingCapacity,
        labor,
        world.Policy.LaborHoursPerOutputUnit,
        productivity,
        hour.Date,
        out var unitCost);
      if (produced.Value <= 0m)
      {
        continue;
      }

      // Labor is shared; reduce remaining allocation roughly by usage
      var usedLabor = produced.Value * world.Policy.LaborHoursPerOutputUnit;
      world.AllocatedLaborHours[plan.Key.Firm] = Math.Max(0m, labor - usedLabor);
      context.State.AppendEvent(new BatchProduced(
        hour, plan.Key.Firm, plan.Key.Facility, plan.Key.Product, produced, unitCost));
    }

    CoreInventoryBridge.ReconcileChanges(world, inventoryBefore);
    return ValueTask.CompletedTask;
  }
}
