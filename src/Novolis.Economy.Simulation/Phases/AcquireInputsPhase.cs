using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class AcquireInputsPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.AcquireInputs;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;

    if (world.Policy.PeriodHours > 0 &&
        hour.HourIndex % world.Policy.PeriodHours == 0)
    {
      world.PendingProcurement.AddRange(world.RecurringProcurement);
      world.PendingExports.AddRange(world.RecurringExports);
    }

    foreach (var order in world.PendingProcurement.OrderBy(o => o.BuyerFirmId.Value).ThenBy(o => o.ProductId.Value))
    {
      if (!world.Ledgers.TryGetValue(order.BuyerFirmId, out var ledger))
      {
        continue;
      }

      var externalLedger = world.ExternalSectorFirmId is { } externalId
        ? world.Ledgers.GetValueOrDefault(externalId)
        : null;
      if (!CanSettleExternalOrder(world, externalLedger))
      {
        continue;
      }

      var affordable = order.MaxUnitPrice.Amount <= 0m
        ? 0m
        : Math.Floor(ledger.Cash.Amount / order.MaxUnitPrice.Amount * 10000m) / 10000m;
      var qty = Math.Min(order.Quantity.Value, affordable);
      if (qty <= 0m)
      {
        continue;
      }

      var quantity = Quantity.From(qty);
      var spend = Money.From(order.MaxUnitPrice.Amount * qty);
      // Stock first under hard caps; only pay for what fits.
      var accepted = CoreInventoryBridge.Add(
        world,
        new InventoryKey(order.BuyerFirmId, order.Destination, order.ProductId),
        new ProductBatch(
          order.ProductId,
          quantity,
          new ProductQuality(100m),
          order.MaxUnitPrice,
          hour.Date,
          BrandId: null));
      if (accepted.Value <= 0m)
      {
        continue;
      }

      spend = Money.From(order.MaxUnitPrice.Amount * accepted.Value);
      LedgerEngine.PostCashPurchase(ledger, spend, hour.Date);
      if (world.MonetaryClosure == MonetaryClosure.ExternalSector &&
          externalLedger is not null)
      {
        LedgerEngine.PostCashSale(externalLedger, spend, Money.Zero, hour.Date);
      }
      world.ExternalTrade.ImportsPaid += spend;
      world.ExternalTrade.ImportsByProduct[order.ProductId] =
        Quantity.From(
          world.ExternalTrade.ImportsByProduct.GetValueOrDefault(order.ProductId).Value +
          accepted.Value);
      context.State.AppendEvent(new ProcurementFilled(
        hour, order.BuyerFirmId, order.ProductId, accepted, order.MaxUnitPrice));
      context.State.AppendEvent(new InventoryTransferred(
        hour, order.BuyerFirmId, order.ProductId, accepted, "exogenous-procurement"));
    }

    world.PendingProcurement.Clear();

    foreach (var order in world.PendingExports.OrderBy(o => o.SellerFirmId.Value).ThenBy(o => o.ProductId.Value))
    {
      if (!world.Ledgers.TryGetValue(order.SellerFirmId, out var ledger))
      {
        continue;
      }

      var externalLedger = world.ExternalSectorFirmId is { } externalId
        ? world.Ledgers.GetValueOrDefault(externalId)
        : null;
      if (!CanSettleExternalOrder(world, externalLedger))
      {
        continue;
      }

      var key = new InventoryKey(order.SellerFirmId, order.Origin, order.ProductId);
      var onHand = world.Inventory.GetQuantity(key);
      var qty = Math.Min(order.Quantity.Value, onHand.Value);
      if (externalLedger is not null && order.MinUnitPrice.Amount > 0m)
      {
        qty = Math.Min(
          qty,
          externalLedger.Cash.Amount / order.MinUnitPrice.Amount);
      }
      if (qty <= 0m || order.MinUnitPrice.Amount < 0m)
      {
        continue;
      }

      var quantity = Quantity.From(qty);
      if (!CoreInventoryBridge.TryTake(world, key, quantity, out _, out var cogs))
      {
        continue;
      }

      var revenue = Money.From(order.MinUnitPrice.Amount * qty);
      LedgerEngine.PostCashSale(ledger, revenue, cogs, hour.Date);
      if (world.MonetaryClosure == MonetaryClosure.ExternalSector &&
          externalLedger is not null)
      {
        LedgerEngine.PostCashPurchase(externalLedger, revenue, hour.Date);
      }
      world.ExternalTrade.ExportsReceived += revenue;
      world.ExternalTrade.ExportsByProduct[order.ProductId] =
        Quantity.From(
          world.ExternalTrade.ExportsByProduct.GetValueOrDefault(order.ProductId).Value +
          quantity.Value);
      context.State.AppendEvent(new ExportFilled(
        hour, order.SellerFirmId, order.ProductId, quantity, order.MinUnitPrice, revenue));
      context.State.AppendEvent(new InventoryTransferred(
        hour, order.SellerFirmId, order.ProductId, quantity, "exogenous-export"));
    }

    world.PendingExports.Clear();

    foreach (var cmd in world.PendingShipments.OrderBy(s => s.FirmId.Value).ThenBy(s => s.ProductId.Value))
    {
      if (!world.Routes.TryGetValue(cmd.RouteId, out var route))
      {
        continue;
      }

      var inventoryBefore = CoreInventoryBridge.Snapshot(world);
      var shipment = LogisticsEngine.TryDepart(
        world.Inventory, cmd.FirmId, route, cmd.ProductId, cmd.Quantity, hour, out _);
      CoreInventoryBridge.ReconcileChanges(world, inventoryBefore);
      if (shipment is null)
      {
        continue;
      }

      world.Shipments.Add(shipment);
      context.State.AppendEvent(new ShipmentDeparted(
        hour, shipment.Id.Value, cmd.FirmId, cmd.ProductId, cmd.Quantity));
    }

    world.PendingShipments.Clear();

    foreach (var cmd in world.PendingPlanShipments.OrderBy(s => s.FirmId.Value).ThenBy(s => s.ProductId.Value))
    {
      var originId = TransportHubId.From(cmd.OriginHubId);
      var destId = TransportHubId.From(cmd.DestinationHubId);
      var vehicleId = VehicleClassId.From(cmd.VehicleClassId);
      if (!world.Hubs.TryGetValue(originId, out var origin) ||
          !world.Hubs.ContainsKey(destId) ||
          !world.VehicleClasses.TryGetValue(vehicleId, out var vehicle))
      {
        world.TransportStats.FailedPlans++;
        context.State.AppendEvent(new ShipmentPlanFailed(hour, cmd.FirmId, cmd.ProductId, "unknown-hub-or-vehicle"));
        continue;
      }

      if (!ItineraryPlanner.TryPlan(
            originId,
            destId,
            cmd.Quantity,
            vehicle,
            world.Corridors,
            out var itinerary,
            TransitProfiles.FromCode(cmd.TransitProfileCode)))
      {
        world.TransportStats.FailedPlans++;
        context.State.AppendEvent(new ShipmentPlanFailed(hour, cmd.FirmId, cmd.ProductId, "no-feasible-path"));
        continue;
      }

      var inventoryBefore = CoreInventoryBridge.Snapshot(world);
      var shipment = LogisticsEngine.TryDepartItinerary(
        world.Inventory,
        cmd.FirmId,
        origin,
        itinerary,
        vehicle,
        cmd.ProductId,
        cmd.Quantity,
        world.TransportFuelProductId,
        hour,
        world.Corridors,
        out _,
        out var failReason,
        TransitProfiles.FromCode(cmd.TransitProfileCode));
      CoreInventoryBridge.ReconcileChanges(world, inventoryBefore);
      if (shipment is null)
      {
        world.TransportStats.FailedPlans++;
        context.State.AppendEvent(new ShipmentPlanFailed(
          hour, cmd.FirmId, cmd.ProductId, failReason ?? "depart-failed"));
        continue;
      }

      world.Shipments.Add(shipment);
      context.State.AppendEvent(new ShipmentDeparted(
        hour, shipment.Id.Value, cmd.FirmId, cmd.ProductId, cmd.Quantity));
    }

    world.PendingPlanShipments.Clear();

    // Sentinel product for empty-hold reposition events (no inventory).
    var emptyHold = LogisticsProductIds.EmptyHold;
    foreach (var cmd in world.PendingPlanRepositions.OrderBy(s => s.FirmId.Value))
    {
      var originId = TransportHubId.From(cmd.OriginHubId);
      var destId = TransportHubId.From(cmd.DestinationHubId);
      var vehicleId = VehicleClassId.From(cmd.VehicleClassId);
      if (!world.Hubs.TryGetValue(originId, out var origin) ||
          !world.Hubs.ContainsKey(destId) ||
          !world.VehicleClasses.TryGetValue(vehicleId, out var vehicle))
      {
        world.TransportStats.FailedPlans++;
        context.State.AppendEvent(new ShipmentPlanFailed(hour, cmd.FirmId, emptyHold, "unknown-hub-or-vehicle"));
        continue;
      }

      var qty = Quantity.Zero;
      if (!ItineraryPlanner.TryPlan(
            originId,
            destId,
            qty,
            vehicle,
            world.Corridors,
            out var itinerary,
            TransitProfiles.FromCode(cmd.TransitProfileCode)))
      {
        world.TransportStats.FailedPlans++;
        context.State.AppendEvent(new ShipmentPlanFailed(hour, cmd.FirmId, emptyHold, "no-feasible-path"));
        continue;
      }

      var inventoryBefore = CoreInventoryBridge.Snapshot(world);
      var shipment = LogisticsEngine.TryDepartItinerary(
        world.Inventory,
        cmd.FirmId,
        origin,
        itinerary,
        vehicle,
        emptyHold,
        qty,
        world.TransportFuelProductId,
        hour,
        world.Corridors,
        out _,
        out var failReason,
        TransitProfiles.FromCode(cmd.TransitProfileCode));
      CoreInventoryBridge.ReconcileChanges(world, inventoryBefore);
      if (shipment is null)
      {
        world.TransportStats.FailedPlans++;
        context.State.AppendEvent(new ShipmentPlanFailed(
          hour, cmd.FirmId, emptyHold, failReason ?? "depart-failed"));
        continue;
      }

      world.Shipments.Add(shipment);
      context.State.AppendEvent(new ShipmentDeparted(
        hour, shipment.Id.Value, cmd.FirmId, emptyHold, qty));
    }

    world.PendingPlanRepositions.Clear();
    return ValueTask.CompletedTask;
  }

  private static bool CanSettleExternalOrder(
    EconomyWorld world,
    FirmLedger? externalLedger) =>
    world.MonetaryClosure switch
    {
      MonetaryClosure.Open => true,
      MonetaryClosure.ExternalSector => externalLedger is not null,
      MonetaryClosure.Closed => false,
      _ => false
    };
}
