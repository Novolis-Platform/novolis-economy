using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Labor;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transport;
using Novolis.Economy.Core.Transactions;
using CoreLegalEntityKind = Novolis.Economy.Core.LegalEntityKind;

using Novolis.Economy.Simulation.Bounded;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>1. Apply policies and opening conditions.</summary>
public sealed class ApplyPolicyStep : IBoundedPeriodStep
{
    public string Name => "01_ApplyPolicy";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        // Reset period scratch + flow ledger; advance period counter at start.
        var state = current with
        {
            Period = checked(current.Period + 1),
            Flows = PeriodFlowLedger.Empty,
            Scratch = BoundedPeriodScratch.Empty
        };

        var policy = state.Policy;
        var fiscal = state.Specification.Fiscal;
        var householdTaxRate = fiscal.HouseholdTaxRate > 0m
            ? fiscal.HouseholdTaxRate
            : policy.HouseholdTaxRate;
        var firmTaxRate = fiscal.FirmTaxRate > 0m
            ? fiscal.FirmTaxRate
            : policy.FirmTaxRate;
        if (policy.TransferPerHousehold.Amount <= 0m &&
            householdTaxRate <= 0m &&
            firmTaxRate <= 0m)
            return state;

        // Find a State entity to act as fiscal counterparty
        var stateEntity = state.Entities.Values.FirstOrDefault(e => e.Kind == CoreLegalEntityKind.State);
        if (stateEntity is null)
            return state;

        // Household transfers (money-conserving: State → household entity or cohort cash)
        if (policy.TransferPerHousehold.Amount > 0m)
        {
            foreach (var cohort in state.Cohorts.Values)
            {
                var total = Money.From(policy.TransferPerHousehold.Amount * cohort.HouseholdCount);
                if (total.Amount <= 0m)
                    continue;
                if (cohort.HouseholdEntityId is { } hid && state.Entities.ContainsKey(hid))
                {
                    if (CashLedger.Balance(state, stateEntity.Id).Amount + 1e-12m < total.Amount)
                        break;
                    state = state.WithEconomy(
                        CashLedger.Transfer(state, stateEntity.Id, hid, total));
                    state = state.WithFlows(state.Flows.RecordTransfer(total));
                }
                else
                {
                    // Credit cash-per-household when no entity link (still debit State)
                    if (CashLedger.Balance(state, stateEntity.Id).Amount + 1e-12m < total.Amount)
                        break;
                    state = state.WithEconomy(
                        CashLedger.Debit(state, stateEntity.Id, total));
                    var cohorts = new Dictionary<CohortId, HouseholdCohort>(state.Cohorts)
                    {
                        [cohort.Id] = cohort with
                        {
                            CashPerHousehold = cohort.CashPerHousehold + policy.TransferPerHousehold
                        }
                    };
                    state = state with { Cohorts = cohorts };
                    state = state.WithFlows(state.Flows.RecordTransfer(total));
                }
            }
        }

        return state;
    }
}

/// <summary>2. Calculate household labor supply.</summary>
public sealed class CalculateLaborSupplyStep : IBoundedPeriodStep
{
    public string Name => "02_CalculateLaborSupply";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var byRegion = new Dictionary<RegionId, decimal>();
        foreach (var regionId in current.Regions.Keys)
            byRegion[regionId] = LaborSupply.Calculate(current, regionId);
        return current with
        {
            Scratch = current.Scratch with { LaborSupplyByRegion = byRegion }
        };
    }
}

/// <summary>3. Allocate regional labor to activities (pro-rata by labor demand).</summary>
public sealed class AllocateLaborStep : IBoundedPeriodStep
{
    public string Name => "03_AllocateLabor";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var allocation = new Dictionary<ActivityId, decimal>();
        foreach (var regionId in current.Regions.Keys)
        {
            var supply = current.Scratch.LaborSupplyByRegion.TryGetValue(regionId, out var s)
                ? s
                : LaborSupply.Calculate(current, regionId);
            var acts = current.Activities.Values.Where(a => a.RegionId.Equals(regionId)).ToList();
            var demand = acts.Sum(a => a.InstalledCapacity * a.Recipe.LaborHoursPerRun);
            if (demand <= 0m || supply <= 0m)
            {
                foreach (var a in acts)
                    allocation[a.Id] = 0m;
                continue;
            }

            var scale = Math.Min(1m, supply / demand);
            foreach (var a in acts)
                allocation[a.Id] = a.InstalledCapacity * a.Recipe.LaborHoursPerRun * scale;
        }

        return current with
        {
            Scratch = current.Scratch with { LaborAllocated = allocation }
        };
    }
}

/// <summary>4. Determine activity production (min constraints).</summary>
public sealed class DetermineProductionStep : IBoundedPeriodStep
{
    public string Name => "04_DetermineProduction";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var runs = new Dictionary<ActivityId, decimal>();
        var committed = new Dictionary<RegionId, decimal>();

        foreach (var activity in current.Activities.Values.OrderBy(a => a.Id.Value))
        {
            var already = committed.GetValueOrDefault(activity.RegionId);
            // Prefer allocated labor ceiling when present
            var allocated = current.Scratch.LaborAllocated.TryGetValue(activity.Id, out var lab)
                ? lab
                : decimal.MaxValue;

            var byLaborAlloc = activity.Recipe.LaborHoursPerRun <= 0m
                ? activity.InstalledCapacity
                : allocated / activity.Recipe.LaborHoursPerRun;

            var calculated = ProductionCalculator.ActualRuns(current, activity, already);
            var actual = Math.Floor(Math.Min(calculated, byLaborAlloc));
            runs[activity.Id] = actual;
            committed[activity.RegionId] = already + actual * activity.Recipe.LaborHoursPerRun;
        }

        return current with
        {
            Scratch = current.Scratch with { ActualRuns = runs }
        };
    }
}

/// <summary>5. Add produced resources to owner holdings.</summary>
public sealed class ApplyProductionStep : IBoundedPeriodStep
{
    public string Name => "05_ApplyProduction";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        foreach (var (activityId, runCount) in current.Scratch.ActualRuns)
        {
            if (runCount <= 0m || !current.Activities.TryGetValue(activityId, out var activity))
                continue;
            state = state.WithEconomy(
                ProductionCalculator.ApplyRuns(state, activity, runCount));
            foreach (var input in activity.Recipe.Inputs)
            {
                if (input.Quantity <= 0m)
                    continue;
                state = state.WithFlows(
                    state.Flows.RecordConsumedQuantity(
                        state.AssetFor(input.ResourceId),
                        input.Quantity * runCount));
            }

            foreach (var output in activity.Recipe.Outputs)
            {
                if (output.Quantity <= 0m)
                    continue;
                state = state.WithFlows(
                    state.Flows.RecordProductionQuantity(
                        state.AssetFor(output.ResourceId),
                        output.Quantity * runCount));
            }
        }

        return state;
    }
}

/// <summary>6. Resolve household and firm demand budgets (scratch only).</summary>
public sealed class ResolveDemandStep : IBoundedPeriodStep
{
    public string Name => "06_ResolveDemand";

    public BoundedPeriodState Execute(BoundedPeriodState current) => current;
}

/// <summary>7. Match buyers and sellers at posted prices (records intended fills in scratch via holdings scan).</summary>
public sealed class MatchBuyersSellersStep : IBoundedPeriodStep
{
    public string Name => "07_MatchBuyersSellers";

    public BoundedPeriodState Execute(BoundedPeriodState current) => current;
}

/// <summary>8. Transfer ownership and payments for matched trades (quantity rationing; no order book).</summary>
public sealed class TransferOwnershipPaymentsStep : IBoundedPeriodStep
{
    public string Name => "08_TransferOwnershipPayments";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        foreach (var cohort in state.Cohorts.Values)
        {
            if (cohort.HouseholdEntityId is not { } buyerId || !state.Entities.ContainsKey(buyerId))
                continue;

            var availableCash = cohort.HouseholdEntityId is { } linkedHousehold &&
                                state.Entities.ContainsKey(linkedHousehold)
                ? CashLedger.Balance(state, linkedHousehold).Amount
                : HouseholdMath.TotalCash(cohort).Amount;
            var budget = Money.From(
                availableCash * Math.Clamp(cohort.Profile.ConsumptionWeight, 0m, 1m));
            budget = Money.From(Math.Min(budget.Amount, CashLedger.Balance(state, buyerId).Amount));

            foreach (var price in state.PostedPrices.Values.Where(p => p.RegionId.Equals(cohort.RegionId)))
            {
                if (budget.Amount <= 0m || price.UnitPrice.Amount <= 0m)
                    continue;
                if (!state.Resources.TryGetValue(price.ResourceId, out var res) ||
                    res.Kind != ResourceKind.ConsumerGood)
                    continue;

                var sellers = PositionLedger.ResourceView(state)
                    .Where(h => h.RegionId.Equals(cohort.RegionId) &&
                                h.ResourceId.Equals(price.ResourceId) &&
                                h.Quantity > 0m &&
                                !h.Owner.Equals(buyerId) &&
                                state.Entities.TryGetValue(h.Owner, out var e) &&
                                e.Kind == CoreLegalEntityKind.Firm)
                    .OrderByDescending(h => h.Quantity)
                    .ToList();

                foreach (var holding in sellers)
                {
                    if (budget.Amount <= 0m)
                        break;
                    var maxByCash = budget.Amount / price.UnitPrice.Amount;
                    var qty = Math.Min(holding.Quantity, maxByCash);
                    if (qty <= 1e-12m)
                        continue;
                    var cost = Money.From(qty * price.UnitPrice.Amount);
                    try
                    {
                        var asset = state.AssetFor(price.ResourceId);
                        state = state.WithEconomy(
                            CashLedger.EnsurePosition(state, buyerId));
                        state = state.WithEconomy(
                            CashLedger.EnsurePosition(state, holding.Owner));
                        state = state.WithEconomy(EconomicTransactionEngine.Apply(
                            state,
                            EconomicTransaction.Create(
                                state,
                                [
                                    new PositionChange(
                                        EconomicIdentity.For(holding.Owner),
                                        asset,
                                        -qty,
                                        cohort.RegionId),
                                    new PositionChange(
                                        EconomicIdentity.For(buyerId),
                                        asset,
                                        qty,
                                        cohort.RegionId),
                                    new PositionChange(
                                        EconomicIdentity.For(buyerId),
                                        state.MonetaryAssetId,
                                        -cost.Amount,
                                        Region: null),
                                    new PositionChange(
                                        EconomicIdentity.For(holding.Owner),
                                        state.MonetaryAssetId,
                                        cost.Amount,
                                        Region: null)
                                ],
                                "posted-price-purchase")));
                        state = state.WithEconomy(PositionLedger.UpdateLegacyResourceProjection(
                            state,
                            holding.Owner,
                            cohort.RegionId,
                            price.ResourceId,
                            holding.Quantity - qty));
                        state = state.WithEconomy(PositionLedger.UpdateLegacyResourceProjection(
                            state,
                            buyerId,
                            cohort.RegionId,
                            price.ResourceId,
                            PositionLedger.GetQuantity(
                                state,
                                EconomicIdentity.For(buyerId),
                                cohort.RegionId,
                                asset)));
                        budget = budget - cost;
                        state = state.WithFlows(state.Flows.RecordCashMoved(cost));
                    }
                    catch (InvalidOperationException)
                    {
                        // skip
                    }
                }
            }
        }

        return state;
    }
}

/// <summary>9. Start pending transfers (already queued) and tick/complete in-flight ones.</summary>
public sealed class ProcessTransfersStep : IBoundedPeriodStep
{
    public string Name => "09_ProcessTransfers";

    public BoundedPeriodState Execute(BoundedPeriodState current) =>
        current.WithEconomy(TransferEngine.TickAndComplete(current));
}

/// <summary>10. Create wage, tax, interest, and insurance obligations.</summary>
public sealed class CreateObligationsStep : IBoundedPeriodStep
{
    public string Name => "10_CreateObligations";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        var due = state.Period;

        // Wages: firm → linked household for labor used
        foreach (var (activityId, runs) in state.Scratch.ActualRuns)
        {
            if (runs <= 0m || !state.Activities.TryGetValue(activityId, out var activity))
                continue;
            var hours = runs * activity.Recipe.LaborHoursPerRun;
            if (hours <= 0m)
                continue;
            var wage = Money.From(hours * state.Policy.WagePerLaborHour.Amount);
            var creditor = FindHouseholdCreditor(state, activity.RegionId);
            if (creditor is null)
                continue;
            state = state.WithEconomy(ObligationEngine.Create(
                state, activity.Operator, creditor.Value, wage, due, ObligationKind.Wage));
            state = state.WithFlows(state.Flows.RecordWages(wage));
        }

        // Interest on performing loans
        var loans = ClaimLedger.LoanView(state)
            .ToDictionary(loan => loan.Id);
        foreach (var loan in loans.Values.Where(l => l.Status == LoanStatus.Performing))
        {
            var interest = Money.From(loan.PrincipalOutstanding.Amount * loan.InterestRatePerPeriod);
            if (interest.Amount <= 0m)
                continue;
            state = state.WithEconomy(ObligationEngine.Create(
                state, loan.Borrower, loan.Lender, interest, due, ObligationKind.Interest));
            // Age remaining periods
            var rem = loan.RemainingPeriods - 1;
            loans[loan.Id] = rem <= 0
                ? loan with { RemainingPeriods = 0 }
                : loan with { RemainingPeriods = rem };
        }

        state = state with { Loans = loans };
        state = state.WithEconomy(ClaimLedger.SyncFromLegacyLoans(state));

        // Principal due when term expired
        foreach (var loan in loans.Values.Where(l =>
                     l.Status == LoanStatus.Performing && l.RemainingPeriods <= 0))
        {
            if (loan.PrincipalOutstanding.Amount <= 0m)
                continue;
            state = state.WithEconomy(ObligationEngine.Create(
                state,
                loan.Borrower,
                loan.Lender,
                loan.PrincipalOutstanding,
                due,
                ObligationKind.Principal));
        }

        // Insurance premiums
        foreach (var cover in state.Insurance)
        {
            if (cover.PremiumPerPeriod.Amount <= 0m)
                continue;
            state = state.WithEconomy(ObligationEngine.Create(
                state,
                cover.Insured,
                cover.Insurer,
                cover.PremiumPerPeriod,
                due,
                ObligationKind.InsurancePremium));
        }

        // Taxes: household and firm rates on cash (simple fiscal). A positive
        // model-specification rate overrides the legacy StatePolicy input.
        var treasury = state.Entities.Values.FirstOrDefault(e => e.Kind == CoreLegalEntityKind.State);
        if (treasury is not null)
        {
            var fiscal = state.Specification.Fiscal;
            var householdTaxRate = fiscal.HouseholdTaxRate > 0m
                ? fiscal.HouseholdTaxRate
                : state.Policy.HouseholdTaxRate;
            var firmTaxRate = fiscal.FirmTaxRate > 0m
                ? fiscal.FirmTaxRate
                : state.Policy.FirmTaxRate;
            foreach (var entity in state.Entities.Values)
            {
                decimal rate = entity.Kind switch
                {
                    CoreLegalEntityKind.Household => householdTaxRate,
                    CoreLegalEntityKind.Firm => firmTaxRate,
                    _ => 0m
                };
                var entityCash = CashLedger.Balance(state, entity.Id);
                if (rate <= 0m || entityCash.Amount <= 0m)
                    continue;
                var tax = Money.From(entityCash.Amount * rate);
                state = state.WithEconomy(ObligationEngine.Create(
                    state, entity.Id, treasury.Id, tax, due, ObligationKind.Tax));
                state = state.WithFlows(state.Flows.RecordTax(tax));
            }
        }

        // Insurance claims from pending losses
        foreach (var loss in state.PendingLosses)
        {
            var covers = state.Insurance
                .Where(c => c.Insured.Equals(loss.Insured) && c.Risk == loss.Risk)
                .ToList();
            foreach (var cover in covers)
            {
                var covered = Money.From(
                    Math.Max(0m, (loss.GrossLoss.Amount - cover.Deductible.Amount) * cover.CoveredFraction));
                if (covered.Amount <= 0m)
                    continue;
                state = state.WithEconomy(ObligationEngine.Create(
                    state, cover.Insurer, cover.Insured, covered, due, ObligationKind.InsuranceClaim));
            }
        }

        return state with
        {
            PendingLosses = Array.Empty<LossEvent>()
        };
    }

    private static LegalEntityId? FindHouseholdCreditor(EconomyState state, RegionId regionId)
    {
        var cohort = state.Cohorts.Values.FirstOrDefault(c =>
            c.RegionId.Equals(regionId) && c.HouseholdEntityId is not null);
        return cohort?.HouseholdEntityId;
    }
}

/// <summary>11. Settle obligations by liquidity and priority.</summary>
public sealed class SettleObligationsStep : IBoundedPeriodStep
{
    public string Name => "11_SettleObligations";

    public BoundedPeriodState Execute(BoundedPeriodState current) =>
        current.WithEconomy(ObligationEngine.SettleDue(current));
}

/// <summary>12. Draw committed credit where liquidity is short.</summary>
public sealed class DrawCreditStep : IBoundedPeriodStep
{
    public string Name => "12_DrawCredit";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        foreach (var facility in current.CreditFacilities.Values.Where(f => f.IsCommitted && f.Available.Amount > 0m))
        {
            var liq = Liquidity.Of(state, facility.Borrower);
            if (liq.Surplus.Amount >= 0m)
                continue;
            var need = Money.From(Math.Min(facility.Available.Amount, -liq.Surplus.Amount));
            if (need.Amount <= 0m)
                continue;
            try
            {
                state = state.WithEconomy(CreditEngine.DrawFacility(
                    state,
                    facility.Id,
                    need,
                    interestRatePerPeriod: state.Specification.Credit.FacilityInterestRatePerPeriod,
                    termPeriods: state.Specification.Credit.FacilityTermPeriods));
            }
            catch (InvalidOperationException)
            {
                // skip
            }
        }

        return state;
    }
}

/// <summary>13. Mark delinquency and default.</summary>
public sealed class MarkDelinquencyStep : IBoundedPeriodStep
{
    public string Name => "13_MarkDelinquency";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var obligations = current.Obligations.ToList();
        for (var i = 0; i < obligations.Count; i++)
        {
            var o = obligations[i];
            if (o.Status != ObligationStatus.Delinquent)
                continue;
            // The declared credit specification controls delinquency duration.
            if (current.Period - o.DuePeriod >=
                current.Specification.Credit.DelinquencyPeriodsBeforeDefault)
                obligations[i] = o with { Status = ObligationStatus.Defaulted };
        }

        var loans = ClaimLedger.LoanView(current)
            .ToDictionary(loan => loan.Id);
        foreach (var loan in loans.Values)
        {
            if (loan.Status is not (LoanStatus.Performing or LoanStatus.Delinquent))
                continue;
            var hasDelinquent = obligations.Any(o =>
                o.Debtor.Equals(loan.Borrower) &&
                o.Creditor.Equals(loan.Lender) &&
                o.Status is ObligationStatus.Delinquent or ObligationStatus.Defaulted &&
                o.Kind is ObligationKind.Interest or ObligationKind.Principal);
            if (!hasDelinquent)
                continue;
            loans[loan.Id] = loan with
            {
                Status = obligations.Any(o =>
                    o.Debtor.Equals(loan.Borrower) && o.Status == ObligationStatus.Defaulted)
                    ? LoanStatus.Defaulted
                    : LoanStatus.Delinquent
            };
        }

        // Mark repaid loans with zero principal and no pending principal/interest
        foreach (var loan in loans.Values.ToList())
        {
            if (loan.PrincipalOutstanding.Amount > 1e-12m)
                continue;
            var pending = obligations.Any(o =>
                o.Debtor.Equals(loan.Borrower) &&
                o.Creditor.Equals(loan.Lender) &&
                o.Status == ObligationStatus.Pending &&
                o.Kind is ObligationKind.Interest or ObligationKind.Principal);
            if (!pending)
                loans[loan.Id] = loan with { Status = LoanStatus.Repaid };
        }

        var next = current with { Obligations = obligations, Loans = loans };
        foreach (var loan in loans.Values)
        {
            var claim = ClaimLedger.Snapshot(next).GetValueOrDefault(
                ClaimLedger.ClaimIdFor(loan.Id));
            if (claim is null || claim.Status == loan.Status)
                continue;
            next = next.WithEconomy(ClaimLedger.Upsert(next, claim with { Status = loan.Status }));
        }

        return next.WithEconomy(ClaimLedger.SyncFromLegacyLoans(next));
    }
}

/// <summary>14. Distribute dividends from firm cash above a retention floor.</summary>
public sealed class DistributeDividendsStep : IBoundedPeriodStep
{
    public string Name => "14_DistributeDividends";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;
        var retention = state.Specification.Dividends.RetainedCashFloor;

        foreach (var firm in state.Entities.Values.Where(e => e.Kind == CoreLegalEntityKind.Firm))
        {
            var distributable = CashLedger.Balance(state, firm.Id).Amount - retention;
            if (distributable <= 0m)
                continue;

            var holdings = state.ShareHoldings.Where(h => h.Issuer.Equals(firm.Id) && h.Units > 0m).ToList();
            var totalUnits = holdings.Sum(h => h.Units);
            if (totalUnits <= 0m)
                continue;

            foreach (var h in holdings)
            {
                var share = Money.From(distributable * (h.Units / totalUnits));
                if (share.Amount <= 0m)
                    continue;
                state = state.WithEconomy(ObligationEngine.Create(
                    state, firm.Id, h.Owner, share, state.Period, ObligationKind.Dividend));
            }
        }

        // Immediately settle dividends created this period
        return state.WithEconomy(ObligationEngine.SettleDue(state));
    }
}

/// <summary>15. Household consumption of holdings + simple migration toward remaining living capacity.</summary>
public sealed class HouseholdConsumeMigrateStep : IBoundedPeriodStep
{
    public string Name => "15_HouseholdConsumeMigrate";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var state = current;

        foreach (var cohort in state.Cohorts.Values)
        {
            if (cohort.HouseholdEntityId is not { } hid)
                continue;
            var consumeRate = Math.Clamp(cohort.Profile.ConsumptionWeight, 0m, 1m);
            var holdings = PositionLedger.ResourceView(state)
                .Where(h => h.Owner.Equals(hid) && h.RegionId.Equals(cohort.RegionId))
                .ToList();
            foreach (var h in holdings)
            {
                if (!state.Resources.TryGetValue(h.ResourceId, out var res) ||
                    res.Kind != ResourceKind.ConsumerGood)
                    continue;
                var eat = h.Quantity * consumeRate;
                if (eat <= 0m)
                    continue;
                state = state.WithEconomy(HoldingLedger.Debit(
                    state, hid, h.RegionId, h.ResourceId, eat));
            }
        }

        // Migration: living overflow OR tax-sensitive mobility when MigrationPreference is high.
        var cohorts = new Dictionary<CohortId, HouseholdCohort>(state.Cohorts);
        var migrated = 0;
        var taxRate = state.Specification.Fiscal.HouseholdTaxRate > 0m
            ? state.Specification.Fiscal.HouseholdTaxRate
            : state.Policy.HouseholdTaxRate;
        var taxPush = taxRate >= state.Specification.Migration.TaxPushThreshold;

        foreach (var cohort in state.Cohorts.Values.ToList())
        {
            if (cohort.Profile.MigrationPreference < 0.5m || cohort.HouseholdCount <= 0)
                continue;
            if (!state.Regions.TryGetValue(cohort.RegionId, out var home))
                continue;
            if (!cohorts.TryGetValue(cohort.Id, out var live) || live.HouseholdCount <= 0)
                continue;

            // Recompute living using current cohort map
            var tempState = state with { Cohorts = cohorts };
            var overflow = RegionCapacity.RemainingLiving(tempState, home) < 0;
            var taxMotivated = taxPush &&
                               cohort.Profile.MigrationPreference >=
                               state.Specification.Migration.MinimumMigrationPreference;
            if (!overflow && !taxMotivated)
                continue;

            var candidates = state.Regions.Values
                .Where(r => !r.Id.Equals(live.RegionId))
                .Select(r => (Region: r, Slack: RegionCapacity.RemainingLiving(tempState, r)))
                .Where(x => x.Slack > 0)
                .OrderByDescending(x => x.Slack)
                .ToList();
            if (candidates.Count == 0)
                continue;

            var target = candidates[0].Region;
            var slack = (int)candidates[0].Slack;
            int move;
            if (overflow)
                move = Math.Min(live.HouseholdCount, slack);
            else
                move = Math.Min(Math.Max(1, live.HouseholdCount / 2), slack);

            if (move <= 0)
                continue;

            if (move >= live.HouseholdCount)
            {
                cohorts[live.Id] = live with { RegionId = target.Id };
                migrated += live.HouseholdCount;
            }
            else
            {
                cohorts[live.Id] = live with { HouseholdCount = live.HouseholdCount - move };
                var splitId = DeterministicIds.CohortSplitIdFor(
                    state,
                    live.Id,
                    target.Id,
                    move);
                cohorts[splitId] = live with { Id = splitId, RegionId = target.Id, HouseholdCount = move };
                migrated += move;
            }
        }

        return state with
        {
            Cohorts = cohorts,
            Scratch = state.Scratch with { HouseholdsMigrated = migrated },
        };
    }
}

/// <summary>16. Reconcile stocks, claims, and ownership.</summary>
public sealed class ReconcileStep : IBoundedPeriodStep
{
    public string Name => "16_Reconcile";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        InvariantChecker.AssertAll(current);
        return current;
    }
}
