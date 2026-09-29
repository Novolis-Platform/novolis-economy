using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class ApplyDecisionsPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.ApplyDecisions;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;
    foreach (var agent in context.Agents.OrderBy(a => a.FirmId.Value))
    {
      cancellationToken.ThrowIfCancellationRequested();
      var random = context.Entropy.Stream($"agents/{agent.FirmId.Value:N}");
      agent.Tick(new AgentContext(
        world,
        hour,
        random,
        context.State.EnqueueCommand,
        context.SimulationHost));
      context.State.AppendAgentDecision(new AgentDecisionTrace(
        hour,
        agent.FirmId,
        agent.GetType().Name,
        agent.LastDecision,
        random.State));
    }

    foreach (var command in context.State.DequeueCommands())
    {
      switch (command)
      {
        case RegisterFirm register:
          if (register.FirmId != default && !string.IsNullOrWhiteSpace(register.Name))
          {
            world.EnsureFirm(register.FirmId, register.Name);
          }

          break;
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
        case PlaceExportOrder export:
          world.PendingExports.Add(export);
          break;
        case IssueShipment shipment:
          world.PendingShipments.Add(shipment);
          break;
        case PlanShipment plan:
          world.PendingPlanShipments.Add(plan);
          break;
        case PlanReposition reposition:
          world.PendingPlanRepositions.Add(reposition);
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
              () => LoanId.From(CreateLoanGuid(world, context.State, originate)));
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
              () => LoanId.From(CreateLoanGuid(world, context.State, originate)));
          }

          if (loan is not null)
          {
            world.Loans.Add(loan);
            CoreClaimBridge.SyncLoan(world, loan);
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
            CoreClaimBridge.HydrateLoan(world, loan);
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
              CoreClaimBridge.SyncLoan(world, loan);
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
            CoreOwnershipBridge.SyncIssuer(world, assign.IssuerFirmId);
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
            CoreOwnershipBridge.SyncIssuer(world, transfer.IssuerFirmId);
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
                     CoreOwnershipBridge.ClaimsFor(world, div.IssuerFirmId).ToList(),
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

    var id = CreateHubOrderId(world, context.State, post);
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

  private static Guid CreateHubOrderId(
    EconomyWorld world,
    SimulationState simulation,
    PostHubOrder post)
  {
    for (var ordinal = 0; ; ordinal++)
    {
      var id = Novolis.Economy.Core.DeterministicIds.GuidFor(
        "hub-order",
        simulation.Seed,
        simulation.Clock.HourIndex,
        world.CoreState.TransitionSequence,
        post.FirmId.Value,
        post.LocationId.Value,
        post.ProductId.Value,
        post.Side,
        post.Quantity.Value,
        post.LimitPrice.Amount,
        ordinal);
      if (world.HubOrders.All(order => order.Id != id))
        return id;
    }
  }

  private static Guid CreateLoanGuid(
    EconomyWorld world,
    SimulationState simulation,
    OriginateLoan request)
  {
    for (var ordinal = 0; ; ordinal++)
    {
      var id = Novolis.Economy.Core.DeterministicIds.GuidFor(
        "simulation-loan",
        simulation.Seed,
        simulation.Clock.HourIndex,
        world.CoreState.TransitionSequence,
        request.LenderFirmId.Value,
        request.BorrowerFirmId.Value,
        request.Principal.Amount,
        request.AnnualInterestRate,
        request.TermHours,
        ordinal);
      if (world.Loans.All(loan => loan.Id.Value != id))
        return id;
    }
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
    if (!CoreInventoryBridge.TryTake(world, sellerKey, xfer.Quantity, out var taken, out var cogs))
    {
      Fail("stock");
      return;
    }

    var buyerKey = new InventoryKey(xfer.BuyerFirmId, xfer.LocationId, xfer.ProductId);
    foreach (var lot in taken)
    {
      CoreInventoryBridge.Add(
        world,
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
