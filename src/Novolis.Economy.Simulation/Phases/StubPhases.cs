using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Logistics;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

/// <summary>Applies queued commands into the world.</summary>
public sealed class ApplyDecisionsPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.ApplyDecisions;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;
    foreach (var command in context.State.DequeueCommands())
    {
      switch (command)
      {
        case SetRetailPrice price:
        {
          var key = (price.FirmId, price.FacilityId, price.ProductId);
          var previous = world.RetailPrices.GetValueOrDefault(key, Money.Zero);
          world.RetailPrices[key] = price.Price;
          context.State.AppendEvent(new RetailPriceChanged(
            hour.Date, price.FirmId, price.FacilityId, price.ProductId, previous, price.Price));
          break;
        }
        case SetProductionPlan plan:
          world.ProductionPlans[(plan.FirmId, plan.FacilityId, plan.ProductId)] = plan.RatePerHour;
          context.State.AppendEvent(new ProductionPlanSet(
            hour, plan.FirmId, plan.FacilityId, plan.ProductId, plan.RatePerHour));
          break;
        case PlaceProcurementOrder order:
          world.PendingProcurement.Add(order);
          break;
        case IssueShipment shipment:
          world.PendingShipments.Add(shipment);
          break;
        case PlanShipment plan:
          world.PendingPlanShipments.Add(plan);
          break;
        case SetAvailableLabor labor:
          world.AvailableLaborHours[labor.FirmId] = labor.HoursPerTick;
          break;
        case AccountingPeriodClose:
          // Handled in CloseAccountingPeriodPhase when due; ignore as immediate command.
          break;
      }
    }

    return ValueTask.CompletedTask;
  }
}

/// <summary>Allocates labor to manufacturing + underway crew and accrues wages.</summary>
public sealed class AllocateLaborPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.AllocateLabor;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    world.AllocatedLaborHours.Clear();

    var crewDemand = new Dictionary<FirmId, decimal>();
    foreach (var shipment in world.Shipments.Where(s => !s.IsLegacy && s.Phase == ShipmentPhase.Underway && s.Vehicle is not null))
    {
      var hours = shipment.Vehicle!.CrewLaborPerUnderwayHour;
      crewDemand[shipment.FirmId] = crewDemand.GetValueOrDefault(shipment.FirmId) + hours;
    }

    foreach (var (firmId, available) in world.AvailableLaborHours.OrderBy(kv => kv.Key.Value))
    {
      var planned = world.ProductionPlans
        .Where(p => p.Key.Firm == firmId)
        .Sum(p => p.Value.Value * world.Policy.LaborHoursPerOutputUnit);
      var crew = crewDemand.GetValueOrDefault(firmId);
      var crewAllocated = Math.Min(crew, available);
      var manufacturingCap = Math.Max(0m, available - crewAllocated);
      var allocated = Math.Min(manufacturingCap, planned);
      world.AllocatedLaborHours[firmId] = allocated;
      var wage = Money.From((allocated + crewAllocated) * world.Policy.WageRatePerHour.Amount);
      if (wage.Amount <= 0m || !world.Ledgers.TryGetValue(firmId, out var ledger))
      {
        continue;
      }

      LedgerEngine.AccrueWages(ledger, wage, context.State.Clock.Date);
      world.AccruedWages[firmId] = world.AccruedWages.GetValueOrDefault(firmId) + wage;
    }

    return ValueTask.CompletedTask;
  }
}

/// <summary>Fills procurement from exogenous supply and dispatches requested shipments.</summary>
public sealed class AcquireInputsPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.AcquireInputs;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;

    foreach (var order in world.PendingProcurement.OrderBy(o => o.BuyerFirmId.Value).ThenBy(o => o.ProductId.Value))
    {
      if (!world.Ledgers.TryGetValue(order.BuyerFirmId, out var ledger))
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
      LedgerEngine.PostCashPurchase(ledger, spend, hour.Date);
      world.Inventory.Add(
        new InventoryKey(order.BuyerFirmId, order.Destination, order.ProductId),
        new ProductBatch(
          order.ProductId,
          quantity,
          new ProductQuality(100m),
          order.MaxUnitPrice,
          hour.Date,
          BrandId: null));
      context.State.AppendEvent(new ProcurementFilled(
        hour, order.BuyerFirmId, order.ProductId, quantity, order.MaxUnitPrice));
      context.State.AppendEvent(new InventoryTransferred(
        hour, order.BuyerFirmId, order.ProductId, quantity, "exogenous-procurement"));
    }

    world.PendingProcurement.Clear();

    foreach (var cmd in world.PendingShipments.OrderBy(s => s.FirmId.Value).ThenBy(s => s.ProductId.Value))
    {
      if (!world.Routes.TryGetValue(cmd.RouteId, out var route))
      {
        continue;
      }

      var shipment = LogisticsEngine.TryDepart(
        world.Inventory, cmd.FirmId, route, cmd.ProductId, cmd.Quantity, hour, out _);
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
            out var itinerary))
      {
        world.TransportStats.FailedPlans++;
        context.State.AppendEvent(new ShipmentPlanFailed(hour, cmd.FirmId, cmd.ProductId, "no-feasible-path"));
        continue;
      }

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
        out var failReason);
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
    return ValueTask.CompletedTask;
  }
}

/// <summary>Advances in-transit shipments (legacy routes and multi-leg hubs).</summary>
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

      return LedgerEngine.TryPostToll(ledger, toll, hour.Date);
    }

    var result = LogisticsEngine.AdvanceHour(
      world.Shipments,
      world.Inventory,
      world.Routes,
      world.Hubs,
      world.Corridors,
      berthUsage,
      TryPayToll,
      world.TransportFuelUnitCost);

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

/// <summary>Runs production plans.</summary>
public sealed class RunProductionPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.RunProduction;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;

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

    return ValueTask.CompletedTask;
  }
}

/// <summary>Auto-restocks retail via configured routes.</summary>
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
        var shipment = LogisticsEngine.TryDepart(
          world.Inventory, facility.FirmId, route, productId, qty, hour, out _);
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

/// <summary>Resolves cohort purchases at posted prices.</summary>
public sealed class ResolveConsumerPurchasesPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.ResolveConsumerPurchases;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    DemandEngine.ResolvePurchases(
      world.Cohorts,
      world.RetailPrices,
      world.RetailFacilityMap(),
      world.Products,
      world.Inventory,
      world.Ledgers,
      context.State.Clock,
      context.State.AppendEvent);

    foreach (var trade in context.State.Events.OfType<MarketTradeObserved>().Where(e => e.Hour.Equals(context.State.Clock)))
    {
      world.MarketBook.RecordTrade(trade.ProductId, trade.Quantity, trade.UnitPrice, trade.Hour);
    }

    return ValueTask.CompletedTask;
  }
}

/// <summary>Pays wages from cash when available.</summary>
public sealed class SettleInvoicesAndWagesPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.SettleInvoicesAndWages;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;

    foreach (var (firmId, accrued) in world.AccruedWages.OrderBy(kv => kv.Key.Value))
    {
      if (accrued.Amount <= 0m || !world.Ledgers.TryGetValue(firmId, out var ledger))
      {
        continue;
      }

      var pay = Money.From(Math.Min(accrued.Amount, ledger.Cash.Amount));
      if (pay.Amount <= 0m)
      {
        continue;
      }

      LedgerEngine.PayWages(ledger, pay, hour.Date);
      world.AccruedWages[firmId] = accrued - pay;
      context.State.AppendEvent(new WagesPaid(hour, firmId, pay));
    }

    foreach (var invoice in world.Invoices.Where(i => !i.IsSettled).OrderBy(i => i.Id))
    {
      if (invoice.BuyerFirmId is not { } buyer || !world.Ledgers.TryGetValue(buyer, out var buyerLedger))
      {
        continue;
      }

      if (!world.Ledgers.TryGetValue(invoice.SellerFirmId, out var sellerLedger))
      {
        continue;
      }

      var pay = Money.From(Math.Min(invoice.Remaining.Amount, buyerLedger.Cash.Amount));
      if (pay.Amount <= 0m)
      {
        continue;
      }

      buyerLedger.Post(AccountRole.AccountsPayable, AccountRole.Cash, pay, hour.Date, "Invoice payment");
      sellerLedger.Post(AccountRole.Cash, AccountRole.AccountsReceivable, pay, hour.Date, "Invoice receipt");
      invoice.Remaining -= pay;
      context.State.AppendEvent(new InvoiceSettled(hour, invoice.Id, pay));
    }

    return ValueTask.CompletedTask;
  }
}

/// <summary>Converts research budget into productivity.</summary>
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

/// <summary>Refreshes market estimates from the trade book.</summary>
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

/// <summary>Closes accounting period and resets cohort budgets.</summary>
public sealed class CloseAccountingPeriodPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.CloseAccountingPeriod;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;
    var period = world.Policy.PeriodHours;
    if (period <= 0 || (hour.HourIndex + 1) % period != 0)
    {
      return ValueTask.CompletedTask;
    }

    foreach (var firmId in world.Firms.Keys.OrderBy(id => id.Value))
    {
      context.State.AppendEvent(new AccountingPeriodClosed(firmId, hour.Date, hour));
    }

    foreach (var cohort in world.Cohorts)
    {
      cohort.ResetBudget();
    }

    return ValueTask.CompletedTask;
  }
}

/// <summary>Emits read-model friendly observations (currently no-op beyond book readiness).</summary>
public sealed class EmitObservationsPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.EmitObservations;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken) =>
    ValueTask.CompletedTask;
}
