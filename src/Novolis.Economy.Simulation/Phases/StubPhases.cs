using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
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
        case TransferGoodsForCash xfer:
          TryTransferGoodsForCash(world, context, hour, xfer);
          break;
        case PostHubOrder post:
          PostOrder(world, context, hour, post);
          break;
        case CancelHubOrder cancel:
        {
          var order = world.HubOrders.FirstOrDefault(o => o.Id == cancel.OrderId);
          if (order is not null)
          {
            world.HubOrders.Remove(order);
          }

          break;
        }
        case OriginateLoan originate:
        {
          if (world.IsCreditFrozen(originate.BorrowerFirmId))
          {
            break;
          }

          Loan? loan = null;
          if (world.IsHousehold(originate.LenderFirmId))
          {
            var cohort = world.FindCohortByHousehold(originate.LenderFirmId);
            if (cohort is null || !world.IsAboveComfort(cohort))
            {
              break;
            }

            var after = cohort.BudgetRemaining.Amount - originate.Principal.Amount;
            if (after <= world.ComfortFloor(cohort).Amount
                || !world.TryDebitHouseholdBudget(originate.LenderFirmId, originate.Principal))
            {
              break;
            }

            loan = LoanEngine.TryOriginateHouseholdLender(
              world.Ledgers,
              originate,
              hour,
              () => LoanId.From(CreateLoanGuid(originate.LenderFirmId, world.Loans.Count)));
            if (loan is null)
            {
              world.CreditHouseholdBudget(originate.LenderFirmId, originate.Principal);
            }
          }
          else
          {
            loan = LoanEngine.TryOriginate(
              world.Ledgers,
              originate,
              hour,
              () => LoanId.From(CreateLoanGuid(originate.LenderFirmId, world.Loans.Count)));
          }

          if (loan is not null)
          {
            world.Loans.Add(loan);
            context.State.AppendEvent(new LoanOriginated(
              hour, loan.Id, loan.LenderFirmId, loan.BorrowerFirmId,
              originate.Principal, loan.AnnualInterestRate, loan.DueAt));
          }

          break;
        }
        case RepayLoan repay:
        {
          var loan = world.Loans.FirstOrDefault(l => l.Id.Equals(repay.LoanId));
          if (loan is not null)
          {
            var householdLender = world.IsHousehold(loan.LenderFirmId);
            var paid = LoanEngine.TryRepay(
              loan,
              world.Ledgers,
              repay.Amount,
              hour,
              householdLender,
              householdLender ? world.CreditHouseholdBudget : null);
            if (paid.Amount > 0m)
            {
              context.State.AppendEvent(new LoanRepaid(hour, loan.Id, paid, loan.PrincipalRemaining));
            }
          }

          break;
        }
        case AssignOwnership assign:
        {
          if (OwnershipEngine.TryAssign(
                world.OwnershipClaims,
                assign.IssuerFirmId,
                assign.OwnerFirmId,
                assign.Fraction,
                world.CanIssueShares))
          {
            context.State.AppendEvent(new OwnershipChanged(
              hour, assign.IssuerFirmId, assign.OwnerFirmId, assign.Fraction));
          }

          break;
        }
        case TransferOwnership transfer:
        {
          if (OwnershipEngine.TryTransfer(
                world.OwnershipClaims,
                transfer.IssuerFirmId,
                transfer.FromOwnerFirmId,
                transfer.ToOwnerFirmId,
                transfer.Fraction,
                world.CanIssueShares))
          {
            var fromFrac = world.OwnershipClaims
              .FirstOrDefault(c =>
                c.IssuerFirmId.Equals(transfer.IssuerFirmId)
                && c.OwnerFirmId.Equals(transfer.FromOwnerFirmId))
              ?.Fraction ?? 0m;
            var toFrac = world.OwnershipClaims
              .FirstOrDefault(c =>
                c.IssuerFirmId.Equals(transfer.IssuerFirmId)
                && c.OwnerFirmId.Equals(transfer.ToOwnerFirmId))
              ?.Fraction ?? 0m;
            context.State.AppendEvent(new OwnershipChanged(
              hour, transfer.IssuerFirmId, transfer.FromOwnerFirmId, fromFrac));
            context.State.AppendEvent(new OwnershipChanged(
              hour, transfer.IssuerFirmId, transfer.ToOwnerFirmId, toFrac));
          }

          break;
        }
        case DeclareDividend div:
        {
          foreach (var (owner, amount) in OwnershipEngine.TryDeclareDividend(
                     world.OwnershipClaims,
                     world.Ledgers,
                     div.IssuerFirmId,
                     div.Total,
                     hour.Date,
                     world.IsHousehold,
                     world.CreditHouseholdBudget))
          {
            context.State.AppendEvent(new DividendPaid(hour, div.IssuerFirmId, owner, amount));
          }

          break;
        }
        case PurchaseOwnership purchase:
        {
          TryPurchaseOwnership(world, context, hour, purchase);
          break;
        }
        case UpgradeFacility upgrade:
        {
          var upgraded = DefaultConsequenceEngine.TryUpgradeFacility(
            world, upgrade, hour, out var failReason);
          if (upgraded is null)
          {
            context.State.AppendEvent(new FacilityUpgradeFailed(
              hour, upgrade.FacilityId, failReason ?? "failed"));
          }
          else
          {
            context.State.AppendEvent(new FacilityUpgraded(
              hour,
              upgraded.Id,
              upgraded.FirmId,
              upgrade.Cost,
              upgrade.CapacityFactor,
              upgraded.ManufacturingCapacity));
          }

          break;
        }
        case AccountingPeriodClose:
          // Handled in CloseAccountingPeriodPhase when due; ignore as immediate command.
          break;
      }
    }

    return ValueTask.CompletedTask;
  }

  private static void TryPurchaseOwnership(
    EconomyWorld world,
    SimulationContext context,
    SimulationHour hour,
    PurchaseOwnership purchase)
  {
    if (purchase.Fraction <= 0m
        || purchase.Price.Amount < 0m
        || !world.CanIssueShares(purchase.IssuerFirmId)
        || !world.Ledgers.TryGetValue(purchase.IssuerFirmId, out var issuerLedger))
    {
      return;
    }

    if (world.IsHousehold(purchase.BuyerFirmId))
    {
      var cohort = world.FindCohortByHousehold(purchase.BuyerFirmId);
      if (cohort is null || !world.IsAboveComfort(cohort))
      {
        return;
      }

      var after = cohort.BudgetRemaining.Amount - purchase.Price.Amount;
      if (after <= world.ComfortFloor(cohort).Amount)
      {
        return;
      }

      if (!OwnershipEngine.TryAddClaim(
            world.OwnershipClaims,
            purchase.IssuerFirmId,
            purchase.BuyerFirmId,
            purchase.Fraction,
            world.CanIssueShares))
      {
        return;
      }

      if (!world.TryDebitHouseholdBudget(purchase.BuyerFirmId, purchase.Price))
      {
        // Roll back claim add by transferring fraction back to zero incrementally.
        OwnershipEngine.TryTransfer(
          world.OwnershipClaims,
          purchase.IssuerFirmId,
          purchase.BuyerFirmId,
          purchase.IssuerFirmId,
          purchase.Fraction,
          world.CanIssueShares);
        var orphan = world.OwnershipClaims.FirstOrDefault(c =>
          c.IssuerFirmId.Equals(purchase.IssuerFirmId)
          && c.OwnerFirmId.Equals(purchase.IssuerFirmId));
        if (orphan is not null)
        {
          world.OwnershipClaims.Remove(orphan);
        }

        return;
      }

      OwnershipEngine.PostOwnershipSaleProceeds(issuerLedger, purchase.Price, hour.Date);
    }
    else
    {
      if (!world.Ledgers.TryGetValue(purchase.BuyerFirmId, out var buyerLedger)
          || buyerLedger.Cash.Amount + 0.0000001m < purchase.Price.Amount)
      {
        return;
      }

      if (!OwnershipEngine.TryAddClaim(
            world.OwnershipClaims,
            purchase.IssuerFirmId,
            purchase.BuyerFirmId,
            purchase.Fraction,
            world.CanIssueShares))
      {
        return;
      }

      buyerLedger.Post(
        AccountRole.Equity, AccountRole.Cash, purchase.Price, hour.Date, "Ownership purchase");
      OwnershipEngine.PostOwnershipSaleProceeds(issuerLedger, purchase.Price, hour.Date);
    }

    var frac = world.OwnershipClaims
      .FirstOrDefault(c =>
        c.IssuerFirmId.Equals(purchase.IssuerFirmId)
        && c.OwnerFirmId.Equals(purchase.BuyerFirmId))
      ?.Fraction ?? purchase.Fraction;
    context.State.AppendEvent(new OwnershipChanged(
      hour, purchase.IssuerFirmId, purchase.BuyerFirmId, frac));
    context.State.AppendEvent(new OwnershipPurchased(
      hour, purchase.IssuerFirmId, purchase.BuyerFirmId, purchase.Fraction, purchase.Price));
  }

  private static void PostOrder(
    EconomyWorld world,
    SimulationContext context,
    SimulationHour hour,
    PostHubOrder post)
  {
    if (post.Quantity.Value <= 0m || post.LimitPrice.Amount < 0m)
    {
      return;
    }

    var id = CreateHubOrderId(post.FirmId, world.HubOrders.Count);
    var order = new HubOrder(
      id,
      post.FirmId,
      post.LocationId,
      post.ProductId,
      post.Side,
      post.Quantity,
      post.LimitPrice,
      hour);
    world.HubOrders.Add(order);
    // Posted/cancelled quotes are high-churn; omit from the event log (fills still emit).
  }

  private static Guid CreateHubOrderId(FirmId firmId, int index)
  {
    var bytes = firmId.Value.ToByteArray();
    var idx = BitConverter.GetBytes(index);
    Buffer.BlockCopy(idx, 0, bytes, 12, 4);
    bytes[15] = 0x0B;
    return new Guid(bytes);
  }

  private static Guid CreateLoanGuid(FirmId firmId, int index)
  {
    var bytes = firmId.Value.ToByteArray();
    var idx = BitConverter.GetBytes(index);
    Buffer.BlockCopy(idx, 0, bytes, 12, 4);
    bytes[15] = 0xF1;
    return new Guid(bytes);
  }

  private static void TryTransferGoodsForCash(
    EconomyWorld world,
    SimulationContext context,
    SimulationHour hour,
    TransferGoodsForCash xfer)
  {
    void Fail(string reason) =>
      context.State.AppendEvent(new TransferGoodsFailed(
        hour, xfer.SellerFirmId, xfer.BuyerFirmId, xfer.ProductId, reason));

    if (xfer.Quantity.Value <= 0m || xfer.UnitPrice.Amount < 0m)
    {
      Fail("invalid");
      return;
    }

    if (!world.Ledgers.TryGetValue(xfer.SellerFirmId, out var sellerLedger)
        || !world.Ledgers.TryGetValue(xfer.BuyerFirmId, out var buyerLedger))
    {
      Fail("ledger");
      return;
    }

    var spend = Money.From(xfer.Quantity.Value * xfer.UnitPrice.Amount);
    if (buyerLedger.Cash.Amount + 0.0000001m < spend.Amount)
    {
      Fail("cash");
      return;
    }

    var sellerKey = new InventoryKey(xfer.SellerFirmId, xfer.LocationId, xfer.ProductId);
    if (!world.Inventory.TryTake(sellerKey, xfer.Quantity, out var taken, out var cogs))
    {
      Fail("stock");
      return;
    }

    var buyerKey = new InventoryKey(xfer.BuyerFirmId, xfer.LocationId, xfer.ProductId);
    foreach (var lot in taken)
    {
      world.Inventory.Add(
        buyerKey,
        lot with { UnitCost = xfer.UnitPrice });
    }

    LedgerEngine.PostCashSale(sellerLedger, spend, cogs, hour.Date);
    LedgerEngine.PostCashPurchase(buyerLedger, spend, hour.Date);
    context.State.AppendEvent(new GoodsSoldInterFirm(
      hour,
      xfer.SellerFirmId,
      xfer.BuyerFirmId,
      xfer.LocationId,
      xfer.ProductId,
      xfer.Quantity,
      xfer.UnitPrice,
      spend));
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

    if (world.RegionLaborPoolsActive)
    {
      ApplyRegionLaborPools(world);
    }

    foreach (var (firmId, available) in world.AvailableLaborHours.OrderBy(kv => kv.Key.Value))
    {
      if (world.IsHousehold(firmId))
      {
        continue;
      }

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

  /// <summary>
  /// Sets firm <see cref="EconomyWorld.AvailableLaborHours"/> from per-area household pools.
  /// </summary>
  public static void ApplyRegionLaborPools(EconomyWorld world)
  {
    var peoplePer = world.Policy.PeoplePerHousehold;
    var pools = new Dictionary<GeographicAreaId, decimal>();
    foreach (var cohort in world.Cohorts)
    {
      var area = cohort.Definition.Area;
      if (!world.Regions.ContainsKey(area))
      {
        continue;
      }

      var hours = HouseholdMath.LaborHoursPerTick(
        cohort.Definition.Population,
        cohort.Definition.Productivity,
        peoplePer);
      pools[area] = pools.GetValueOrDefault(area) + hours;
    }

    var remaining = pools.ToDictionary(kv => kv.Key, kv => kv.Value);
    var firmAvailable = new Dictionary<FirmId, decimal>();

    // Manufacturing demand by area → firm (deterministic firm order).
    var demandByArea = new Dictionary<GeographicAreaId, List<(FirmId Firm, decimal Hours)>>();
    foreach (var plan in world.ProductionPlans.OrderBy(p => p.Key.Firm.Value).ThenBy(p => p.Key.Facility.Value))
    {
      if (!world.Facilities.TryGetValue(plan.Key.Facility, out var facility)
          || facility.Area is not { } area
          || !world.Regions.ContainsKey(area))
      {
        continue;
      }

      var hours = plan.Value.Value * world.Policy.LaborHoursPerOutputUnit;
      if (hours <= 0m)
      {
        continue;
      }

      if (!demandByArea.TryGetValue(area, out var list))
      {
        list = [];
        demandByArea[area] = list;
      }

      var idx = list.FindIndex(x => x.Firm.Equals(plan.Key.Firm));
      if (idx < 0)
      {
        list.Add((plan.Key.Firm, hours));
      }
      else
      {
        list[idx] = (plan.Key.Firm, list[idx].Hours + hours);
      }
    }

    foreach (var area in demandByArea.Keys.OrderBy(a => a.Value))
    {
      var poolLeft = remaining.GetValueOrDefault(area);
      foreach (var (firm, hours) in demandByArea[area].OrderBy(x => x.Firm.Value))
      {
        var take = Math.Min(hours, poolLeft);
        if (take <= 0m)
        {
          continue;
        }

        firmAvailable[firm] = firmAvailable.GetValueOrDefault(firm) + take;
        poolLeft -= take;
      }

      remaining[area] = poolLeft;
    }

    // Firms with region-only mfg demand get pool supply (legacy SetLabor ignored for those).
    // Crew-only firms (carriers with storage posts) keep SetLabor.
    foreach (var firmId in world.Firms.Keys.OrderBy(f => f.Value))
    {
      if (world.IsHousehold(firmId))
      {
        world.AvailableLaborHours[firmId] = 0m;
        continue;
      }

      if (firmAvailable.TryGetValue(firmId, out var fromPool))
      {
        world.AvailableLaborHours[firmId] = fromPool;
        continue;
      }

      var facilities = world.Facilities.Values.Where(f => f.FirmId.Equals(firmId)).ToList();
      var hasRegionMfg = facilities.Any(f =>
        f.Area is { } a
        && world.Regions.ContainsKey(a)
        && EconomicRegion.ConsumesProductionSlot(f.Layout));
      if (hasRegionMfg)
      {
        world.AvailableLaborHours[firmId] = 0m;
      }
      // else keep builder / SetAvailableLabor value for crew-only operators
    }
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

/// <summary>Matches hub spot buy and sell orders at the same location.</summary>
public sealed class MatchHubOrdersPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.MatchHubOrders;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;
    var open = world.HubOrders.Where(o => !o.IsFilled).ToList();
    var groups = open
      .GroupBy(o => (o.LocationId, o.ProductId))
      .OrderBy(g => g.Key.LocationId.Value)
      .ThenBy(g => g.Key.ProductId.Value);

    foreach (var group in groups)
    {
      var buys = group
        .Where(o => o.Side == HubOrderSide.Buy)
        .OrderByDescending(o => o.LimitPrice.Amount)
        .ThenBy(o => o.PostedAt.HourIndex)
        .ThenBy(o => o.Id)
        .ToList();
      var sells = group
        .Where(o => o.Side == HubOrderSide.Sell)
        .OrderBy(o => o.LimitPrice.Amount)
        .ThenBy(o => o.PostedAt.HourIndex)
        .ThenBy(o => o.Id)
        .ToList();

      var bi = 0;
      var si = 0;
      while (bi < buys.Count && si < sells.Count)
      {
        var buy = buys[bi];
        var sell = sells[si];
        if (buy.FirmId.Equals(sell.FirmId))
        {
          si++;
          continue;
        }

        if (buy.LimitPrice.Amount + 0.0000001m < sell.LimitPrice.Amount)
        {
          break;
        }

        var fillQty = Math.Min(buy.Remaining.Value, sell.Remaining.Value);
        if (fillQty <= 0m)
        {
          if (buy.Remaining.Value <= 0m) bi++;
          if (sell.Remaining.Value <= 0m) si++;
          continue;
        }

        var unitPrice = sell.LimitPrice; // maker sell price (deterministic)
        var spend = Money.From(fillQty * unitPrice.Amount);
        if (!world.Ledgers.TryGetValue(buy.FirmId, out var buyerLedger)
            || !world.Ledgers.TryGetValue(sell.FirmId, out var sellerLedger)
            || buyerLedger.Cash.Amount + 0.0000001m < spend.Amount)
        {
          bi++;
          continue;
        }

        var sellerKey = new InventoryKey(sell.FirmId, sell.LocationId, sell.ProductId);
        if (!world.Inventory.TryTake(sellerKey, Quantity.From(fillQty), out var taken, out var cogs))
        {
          si++;
          continue;
        }

        var buyerKey = new InventoryKey(buy.FirmId, buy.LocationId, buy.ProductId);
        foreach (var lot in taken)
        {
          world.Inventory.Add(buyerKey, lot with { UnitCost = unitPrice });
        }

        LedgerEngine.PostCashSale(sellerLedger, spend, cogs, hour.Date);
        LedgerEngine.PostCashPurchase(buyerLedger, spend, hour.Date);

        buy.Remaining = Quantity.From(buy.Remaining.Value - fillQty);
        sell.Remaining = Quantity.From(sell.Remaining.Value - fillQty);

        context.State.AppendEvent(new HubOrderFilled(
          hour, buy.Id, sell.Id, buy.FirmId, sell.FirmId,
          buy.LocationId, buy.ProductId, Quantity.From(fillQty), unitPrice));
        context.State.AppendEvent(new GoodsSoldInterFirm(
          hour, sell.FirmId, buy.FirmId, buy.LocationId, buy.ProductId,
          Quantity.From(fillQty), unitPrice, spend));
        world.MarketBook.RecordTrade(buy.ProductId, Quantity.From(fillQty), unitPrice, hour);

        if (buy.IsFilled) bi++;
        if (sell.IsFilled) si++;
      }
    }

    world.HubOrders.RemoveAll(o => o.IsFilled);
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
      e =>
      {
        context.State.AppendEvent(e);
        if (e is MarketTradeObserved trade)
        {
          world.MarketBook.RecordTrade(trade.ProductId, trade.Quantity, trade.UnitPrice, trade.Hour);
        }
      },
      world.Policy.PriceElasticity);

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

      if (world.Policy.HouseholdCreditFromWages && pay.Amount > 0m)
      {
        DistributeWageCreditsForFirm(world, firmId, pay.Amount);
        context.State.AppendEvent(new HouseholdCreditsIssued(hour, firmId, pay));
      }
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

  /// <summary>
  /// Credits cohorts by facility area when the paying firm has area-bound facilities;
  /// otherwise population-weighted global fallback.
  /// </summary>
  public static void DistributeWageCreditsForFirm(EconomyWorld world, FirmId firmId, decimal amount)
  {
    if (amount <= 0m || world.Cohorts.Count == 0)
    {
      return;
    }

    var facilities = world.Facilities.Values.Where(f => f.FirmId.Equals(firmId)).ToList();
    var areaWeights = facilities
      .Where(f => f.Area is not null)
      .GroupBy(f => f.Area!.Value)
      .Select(g => (Area: g.Key, Weight: Math.Max(1m, g.Sum(f => f.ManufacturingCapacity.Value))))
      .ToList();

    if (areaWeights.Count == 0)
    {
      DistributeWageCreditsToCohorts(world, amount);
      return;
    }

    var totalWeight = areaWeights.Sum(a => a.Weight);
    var allocated = 0m;
    for (var i = 0; i < areaWeights.Count; i++)
    {
      var (area, weight) = areaWeights[i];
      decimal share;
      if (i == areaWeights.Count - 1)
      {
        share = amount - allocated;
      }
      else
      {
        share = Math.Round(amount * weight / totalWeight, 4, MidpointRounding.AwayFromZero);
        allocated += share;
      }

      if (share > 0m)
      {
        DistributeWageCreditsToCohortsInArea(world, area, share);
      }
    }
  }

  /// <summary>Population-weighted split within one area; falls back globally if empty.</summary>
  internal static void DistributeWageCreditsToCohortsInArea(
    EconomyWorld world,
    GeographicAreaId area,
    decimal amount)
  {
    var local = world.Cohorts.Where(c => c.Definition.Area.Equals(area)).ToList();
    if (local.Count == 0)
    {
      DistributeWageCreditsToCohorts(world, amount);
      return;
    }

    CreditCohortsPopulationWeighted(local, amount);
  }

  /// <summary>Population-weighted split; remainder to largest cohort (stable id tie-break).</summary>
  internal static void DistributeWageCreditsToCohorts(EconomyWorld world, decimal amount)
  {
    if (amount <= 0m || world.Cohorts.Count == 0)
    {
      return;
    }

    CreditCohortsPopulationWeighted(world.Cohorts, amount);
  }

  private static void CreditCohortsPopulationWeighted(IReadOnlyList<CohortState> cohorts, decimal amount)
  {
    var popTotal = cohorts.Sum(c => Math.Max(1, c.Definition.Population.Value));
    if (popTotal <= 0 || amount <= 0m)
    {
      return;
    }

    var allocated = 0m;
    var ordered = cohorts
      .OrderByDescending(c => c.Definition.Population.Value)
      .ThenBy(c => c.Definition.Id.Value)
      .ToList();

    for (var i = 0; i < ordered.Count; i++)
    {
      var c = ordered[i];
      decimal share;
      if (i == ordered.Count - 1)
      {
        share = amount - allocated;
      }
      else
      {
        share = Math.Round(
          amount * c.Definition.Population.Value / popTotal,
          4,
          MidpointRounding.AwayFromZero);
        allocated += share;
      }

      if (share > 0m)
      {
        c.BudgetRemaining = Money.From(c.BudgetRemaining.Amount + share);
      }
    }
  }
}

/// <summary>Accrues loan interest and settles / defaults loans at term.</summary>
public sealed class SettleFinancePhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.SettleFinance;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;

    foreach (var loan in world.Loans.Where(l => l.Status == LoanStatus.Active).OrderBy(l => l.Id.Value))
    {
      var interest = LoanEngine.AccrueHour(loan, world.Ledgers, hour);
      if (interest.Amount > 0m)
      {
        context.State.AppendEvent(new InterestAccrued(hour, loan.Id, interest));
      }

      if (hour.HourIndex < loan.DueAt.HourIndex)
      {
        continue;
      }

      var owed = loan.PrincipalRemaining;
      var householdLender = world.IsHousehold(loan.LenderFirmId);
      var paid = LoanEngine.TryRepay(
        loan,
        world.Ledgers,
        owed,
        hour,
        householdLender,
        householdLender ? world.CreditHouseholdBudget : null);
      if (paid.Amount > 0m)
      {
        context.State.AppendEvent(new LoanRepaid(hour, loan.Id, paid, loan.PrincipalRemaining));
      }

      if (loan.Status == LoanStatus.Active && loan.PrincipalRemaining.Amount > 0.0000001m)
      {
        loan.Status = LoanStatus.Defaulted;
        context.State.AppendEvent(new LoanDefaulted(
          hour, loan.Id, loan.BorrowerFirmId, loan.PrincipalRemaining));
        DefaultConsequenceEngine.ApplyAbsorb(
          world,
          loan.LenderFirmId,
          loan.BorrowerFirmId,
          hour,
          context.State.AppendEvent);
      }
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

    if (world.Policy.CohortBudgetResetMode == CohortBudgetResetMode.CarryForward)
    {
      return ValueTask.CompletedTask;
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
