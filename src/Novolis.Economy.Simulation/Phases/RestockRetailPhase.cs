using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class RestockRetailPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.RestockRetail;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;
    foreach (var (facilityId, routeId) in world.RestockRoutes.OrderBy(kv => kv.Key.Value))
    {
      if (!world.Facilities.TryGetValue(facilityId, out var facility) ||
          !world.Routes.TryGetValue(routeId, out var route) ||
          facility.RetailLocation is null)
      {
        continue;
      }

      // Move finished goods that have retail prices
      var products = world.RetailPrices
        .Where(p => p.Key.Firm == facility.FirmId && p.Key.Facility == facilityId)
        .Select(p => p.Key.Product)
        .Distinct()
        .OrderBy(p => p.Value);

      foreach (var productId in products)
      {
        var key = new InventoryKey(facility.FirmId, facility.StorageLocation, productId);
        var available = world.Inventory.GetQuantity(key);
        if (available.Value <= 0m)
        {
          continue;
        }

        // Ship up to route capacity
        var qty = Quantity.From(Math.Min(available.Value, route.Capacity.Value));
        var inventoryBefore = CoreInventoryBridge.Snapshot(world);
        var shipment = LogisticsEngine.TryDepart(
          world.Inventory, facility.FirmId, route, productId, qty, hour, out _);
        CoreInventoryBridge.ReconcileChanges(world, inventoryBefore);
        if (shipment is null)
        {
          continue;
        }

        world.Shipments.Add(shipment);
        context.State.AppendEvent(new ShipmentDeparted(
          hour, shipment.Id.Value, facility.FirmId, productId, qty));
      }
    }

    return ValueTask.CompletedTask;
  }
}
