using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class TransportInventoryPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.TransportInventory;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;
    var berthUsage = new Dictionary<TransportHubId, int>();

    bool TryPayToll(FirmId firmId, Money toll)
    {
      if (!world.Ledgers.TryGetValue(firmId, out var ledger))
      {
        return false;
      }

      if (!LedgerEngine.TryPostToll(ledger, toll, hour.Date))
      {
        return false;
      }

      var beneficiary = world.Policy.TollBeneficiaryFirmId;
      if (beneficiary is { } treasuryId
          && treasuryId != firmId
          && toll.Amount > 0m
          && world.Ledgers.TryGetValue(treasuryId, out var treasury))
      {
        treasury.Post(
          AccountRole.Cash,
          AccountRole.Revenue,
          toll,
          hour.Date,
          "Corridor toll");
      }

      return true;
    }

    var inventoryBefore = CoreInventoryBridge.Snapshot(world);
    var result = LogisticsEngine.AdvanceHour(
      world.Shipments,
      world.Inventory,
      world.Routes,
      world.Hubs,
      world.Corridors,
      berthUsage,
      TryPayToll,
      world.TransportFuelUnitCost);
    CoreInventoryBridge.ReconcileChanges(world, inventoryBefore);

    foreach (var (shipment, corridorId) in result.LegStarts)
    {
      context.State.AppendEvent(new ShipmentLegStarted(
        hour, shipment.Id.Value, shipment.FirmId, corridorId.Value));
    }

    foreach (var (shipment, hubId) in result.HubArrivals)
    {
      context.State.AppendEvent(new ShipmentHubArrived(
        hour, shipment.Id.Value, shipment.FirmId, hubId.Value));
    }

    if (result.FuelBunkered.Value > 0m && world.TransportFuelProductId is { } fuelId)
    {
      var sample = result.HubArrivals.FirstOrDefault().Shipment
        ?? result.LegStarts.FirstOrDefault().Shipment
        ?? result.Delivered.FirstOrDefault();
      context.State.AppendEvent(new FuelBunkered(
        hour,
        sample?.Id.Value ?? Guid.Empty,
        sample?.FirmId ?? FirmId.From(Guid.Empty),
        fuelId,
        result.FuelBunkered));
    }

    if (result.TollsPaid.Amount > 0m)
    {
      var sample = result.LegStarts.FirstOrDefault().Shipment;
      context.State.AppendEvent(new TransportTollPaid(
        hour,
        sample?.Id.Value ?? Guid.Empty,
        sample?.FirmId ?? FirmId.From(Guid.Empty),
        result.TollsPaid));
    }

    foreach (var (firmId, burnValue) in result.FuelBurnValueByFirm.OrderBy(kv => kv.Key.Value))
    {
      if (burnValue.Amount > 0m && world.Ledgers.TryGetValue(firmId, out var ledger))
      {
        LedgerEngine.PostFuelBurn(ledger, burnValue, hour.Date);
      }
    }

    world.TransportStats.FuelBurned = Quantity.From(world.TransportStats.FuelBurned.Value + result.FuelBurned.Value);
    world.TransportStats.FuelBurnValue = Money.From(world.TransportStats.FuelBurnValue.Amount + result.FuelBurnValue.Amount);
    world.TransportStats.FuelBunkered = Quantity.From(world.TransportStats.FuelBunkered.Value + result.FuelBunkered.Value);
    world.TransportStats.TollsPaid = Money.From(world.TransportStats.TollsPaid.Amount + result.TollsPaid.Amount);
    world.TransportStats.CrewLaborHours += result.CrewLaborByFirm.Values.Sum();
    world.TransportStats.DriveWearAccumulated += result.DriveWear;

    foreach (var shipment in result.Delivered)
    {
      context.State.AppendEvent(new ShipmentDelivered(
        hour, shipment.Id.Value, shipment.FirmId, shipment.ProductId, shipment.Quantity));
      context.State.AppendEvent(new InventoryTransferred(
        hour, shipment.FirmId, shipment.ProductId, shipment.Quantity, "shipment-delivery"));

      if (!shipment.IsLegacy)
      {
        world.TransportStats.CargoDelivered = Quantity.From(
          world.TransportStats.CargoDelivered.Value + shipment.Quantity.Value);
        var transit = hour.HourIndex - shipment.DepartedAt.HourIndex + 1;
        world.TransportStats.TransitHoursSum += transit;
        world.TransportStats.TransitSampleCount++;
      }
    }

    world.Shipments.RemoveAll(s =>
      s.Status is ShipmentStatus.Delivered or ShipmentStatus.Cancelled ||
      s.Phase is ShipmentPhase.Delivered or ShipmentPhase.Cancelled);
    return ValueTask.CompletedTask;
  }
}
