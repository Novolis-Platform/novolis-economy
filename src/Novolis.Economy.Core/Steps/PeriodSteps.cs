using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Labor;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transport;

namespace Novolis.Economy.Core.Steps;

/// <summary>1. Apply policies and opening conditions.</summary>
public sealed class ApplyPolicyStep : IEconomyStep
{
    public string Name => "01_ApplyPolicy";

    public EconomyState Execute(EconomyState current)
    {
        // Reset period scratch + flow ledger; advance period counter at start.
        var state = current with
        {
            Period = checked(current.Period + 1),
            Flows = PeriodFlowLedger.Empty,
            Scratch = PeriodScratch.Empty
        };

        var policy = state.Policy;
        if (policy.TransferPerHousehold.Amount <= 0m &&
            policy.HouseholdTaxRate <= 0m &&
            policy.FirmTaxRate <= 0m)
            return state;

        // Find a State entity to act as fiscal counterparty
        var stateEntity = state.Entities.Values.FirstOrDefault(e => e.Kind == LegalEntityKind.State);
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
                    if (state.Entities[stateEntity.Id].Cash.Amount + 1e-12m < total.Amount)
                        break;
                    state = CashLedger.Transfer(state, stateEntity.Id, hid, total);
                    state = state.WithFlows(state.Flows.RecordTransfer(total));
                }
                else
                {
                    // Credit cash-per-household when no entity link (still debit State)
                    if (state.Entities[stateEntity.Id].Cash.Amount + 1e-12m < total.Amount)
                        break;
                    state = CashLedger.Debit(state, stateEntity.Id, total);
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
public sealed class CalculateLaborSupplyStep : IEconomyStep
{
    public string Name => "02_CalculateLaborSupply";

    public EconomyState Execute(EconomyState current)
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
public sealed class AllocateLaborStep : IEconomyStep
{
    public string Name => "03_AllocateLabor";

    public EconomyState Execute(EconomyState current)
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
public sealed class DetermineProductionStep : IEconomyStep
{
    public string Name => "04_DetermineProduction";

    public EconomyState Execute(EconomyState current)
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
public sealed class ApplyProductionStep : IEconomyStep
{
    public string Name => "05_ApplyProduction";

    public EconomyState Execute(EconomyState current)
    {
        var state = current;
        foreach (var (activityId, runCount) in current.Scratch.ActualRuns)
        {
            if (runCount <= 0m || !current.Activities.TryGetValue(activityId, out var activity))
                continue;
            state = ProductionCalculator.ApplyRuns(state, activity, runCount);
            var outputQty = activity.Recipe.Outputs.Sum(o => o.Quantity * runCount);
            state = state.WithFlows(state.Flows.RecordProduction(Money.From(outputQty)));
        }

        return state;
    }
}

/// <summary>6. Resolve household and firm demand budgets (scratch only).</summary>
public sealed class ResolveDemandStep : IEconomyStep
{
    public string Name => "06_ResolveDemand";

    public EconomyState Execute(EconomyState current) => current;
}

/// <summary>7. Match buyers and sellers at posted prices (records intended fills in scratch via holdings scan).</summary>
public sealed class MatchBuyersSellersStep : IEconomyStep
{
    public string Name => "07_MatchBuyersSellers";

    public EconomyState Execute(EconomyState current) => current;
}

/// <summary>8. Transfer ownership and payments for matched trades (quantity rationing; no order book).</summary>
public sealed class TransferOwnershipPaymentsStep : IEconomyStep
{
    public string Name => "08_TransferOwnershipPayments";

    public EconomyState Execute(EconomyState current)
    {
        var state = current;
        foreach (var cohort in state.Cohorts.Values)
        {
            if (cohort.HouseholdEntityId is not { } buyerId || !state.Entities.ContainsKey(buyerId))
                continue;

            var budget = Money.From(
                HouseholdMath.TotalCash(cohort).Amount * Math.Clamp(cohort.Profile.ConsumptionWeight, 0m, 1m));
            budget = Money.From(Math.Min(budget.Amount, state.Entities[buyerId].Cash.Amount));

            foreach (var price in state.PostedPrices.Values.Where(p => p.RegionId.Equals(cohort.RegionId)))
            {
                if (budget.Amount <= 0m || price.UnitPrice.Amount <= 0m)
                    continue;
                if (!state.Resources.TryGetValue(price.ResourceId, out var res) ||
                    res.Kind != ResourceKind.ConsumerGood)
                    continue;

                var sellers = state.Holdings.Values
                    .Where(h => h.RegionId.Equals(cohort.RegionId) &&
                                h.ResourceId.Equals(price.ResourceId) &&
                                h.Quantity > 0m &&
                                !h.Owner.Equals(buyerId) &&
                                state.Entities.TryGetValue(h.Owner, out var e) &&
                                e.Kind == LegalEntityKind.Firm)
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
                        state = HoldingLedger.TransferOwnership(
                            state, holding.Owner, buyerId, cohort.RegionId, price.ResourceId, qty);
                        state = CashLedger.Transfer(state, buyerId, holding.Owner, cost);
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
public sealed class ProcessTransfersStep : IEconomyStep
{
    public string Name => "09_ProcessTransfers";

    public EconomyState Execute(EconomyState current) =>
        TransferEngine.TickAndComplete(current);
}

/// <summary>10. Create wage, tax, interest, and insurance obligations.</summary>
public sealed class CreateObligationsStep : IEconomyStep
{
    public string Name => "10_CreateObligations";

    public EconomyState Execute(EconomyState current)
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
            state = ObligationEngine.Create(
                state, activity.Operator, creditor.Value, wage, due, ObligationKind.Wage);
            state = state.WithFlows(state.Flows.RecordWages(wage));
        }

        // Interest on performing loans
        var loans = new Dictionary<LoanId, Loan>(state.Loans);
        foreach (var loan in state.Loans.Values.Where(l => l.Status == LoanStatus.Performing))
        {
            var interest = Money.From(loan.PrincipalOutstanding.Amount * loan.InterestRatePerPeriod);
            if (interest.Amount <= 0m)
                continue;
            state = ObligationEngine.Create(
                state, loan.Borrower, loan.Lender, interest, due, ObligationKind.Interest);
            // Age remaining periods
            var rem = loan.RemainingPeriods - 1;
            loans[loan.Id] = rem <= 0
                ? loan with { RemainingPeriods = 0 }
                : loan with { RemainingPeriods = rem };
        }

        state = state with { Loans = loans };

        // Principal due when term expired
        foreach (var loan in state.Loans.Values.Where(l =>
                     l.Status == LoanStatus.Performing && l.RemainingPeriods <= 0))
        {
            if (loan.PrincipalOutstanding.Amount <= 0m)
                continue;
            state = ObligationEngine.Create(
                state,
                loan.Borrower,
                loan.Lender,
                loan.PrincipalOutstanding,
                due,
                ObligationKind.Principal);
        }

        // Insurance premiums
        foreach (var cover in state.Insurance)
        {
            if (cover.PremiumPerPeriod.Amount <= 0m)
                continue;
            state = ObligationEngine.Create(
                state,
                cover.Insured,
                cover.Insurer,
                cover.PremiumPerPeriod,
                due,
                ObligationKind.InsurancePremium);
        }

        // Taxes: household and firm rates on cash (simple fiscal)
        var treasury = state.Entities.Values.FirstOrDefault(e => e.Kind == LegalEntityKind.State);
        if (treasury is not null)
        {
            foreach (var entity in state.Entities.Values)
            {
                decimal rate = entity.Kind switch
                {
                    LegalEntityKind.Household => state.Policy.HouseholdTaxRate,
                    LegalEntityKind.Firm => state.Policy.FirmTaxRate,
                    _ => 0m
                };
                if (rate <= 0m || entity.Cash.Amount <= 0m)
                    continue;
                var tax = Money.From(entity.Cash.Amount * rate);
                state = ObligationEngine.Create(
                    state, entity.Id, treasury.Id, tax, due, ObligationKind.Tax);
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
                state = ObligationEngine.Create(
                    state, cover.Insurer, cover.Insured, covered, due, ObligationKind.InsuranceClaim);
            }
        }

        return state with { PendingLosses = Array.Empty<LossEvent>() };
    }

    private static LegalEntityId? FindHouseholdCreditor(EconomyState state, RegionId regionId)
    {
        var cohort = state.Cohorts.Values.FirstOrDefault(c =>
            c.RegionId.Equals(regionId) && c.HouseholdEntityId is not null);
        return cohort?.HouseholdEntityId;
    }
}

/// <summary>11. Settle obligations by liquidity and priority.</summary>
public sealed class SettleObligationsStep : IEconomyStep
{
    public string Name => "11_SettleObligations";

    public EconomyState Execute(EconomyState current) =>
        ObligationEngine.SettleDue(current);
}

/// <summary>12. Draw committed credit where liquidity is short.</summary>
public sealed class DrawCreditStep : IEconomyStep
{
    public string Name => "12_DrawCredit";

    public EconomyState Execute(EconomyState current)
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
                state = CreditEngine.DrawFacility(state, facility.Id, need, interestRatePerPeriod: 0.01m, termPeriods: 4);
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
public sealed class MarkDelinquencyStep : IEconomyStep
{
    public string Name => "13_MarkDelinquency";

    public EconomyState Execute(EconomyState current)
    {
        var obligations = current.Obligations.ToList();
        for (var i = 0; i < obligations.Count; i++)
        {
            var o = obligations[i];
            if (o.Status != ObligationStatus.Delinquent)
                continue;
            // Already delinquent for > 2 periods past due → defaulted
            if (current.Period - o.DuePeriod >= 2)
                obligations[i] = o with { Status = ObligationStatus.Defaulted };
        }

        var loans = new Dictionary<LoanId, Loan>(current.Loans);
        foreach (var loan in current.Loans.Values)
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

        return current with { Obligations = obligations, Loans = loans };
    }
}

/// <summary>14. Distribute dividends from firm cash above a retention floor.</summary>
public sealed class DistributeDividendsStep : IEconomyStep
{
    public string Name => "14_DistributeDividends";

    public EconomyState Execute(EconomyState current)
    {
        var state = current;
        const decimal retention = 10m;

        foreach (var firm in state.Entities.Values.Where(e => e.Kind == LegalEntityKind.Firm))
        {
            var distributable = firm.Cash.Amount - retention;
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
                state = ObligationEngine.Create(
                    state, firm.Id, h.Owner, share, state.Period, ObligationKind.Dividend);
            }
        }

        // Immediately settle dividends created this period
        return ObligationEngine.SettleDue(state);
    }
}

/// <summary>15. Household consumption of holdings + simple migration toward remaining living capacity.</summary>
public sealed class HouseholdConsumeMigrateStep : IEconomyStep
{
    public string Name => "15_HouseholdConsumeMigrate";

    public EconomyState Execute(EconomyState current)
    {
        var state = current;

        foreach (var cohort in state.Cohorts.Values)
        {
            if (cohort.HouseholdEntityId is not { } hid)
                continue;
            var consumeRate = Math.Clamp(cohort.Profile.ConsumptionWeight, 0m, 1m);
            var holdings = state.Holdings.Values
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
                state = HoldingLedger.Debit(state, hid, h.RegionId, h.ResourceId, eat);
            }
        }

        // Migration: living overflow OR tax-sensitive mobility when MigrationPreference is high.
        var cohorts = new Dictionary<CohortId, HouseholdCohort>(state.Cohorts);
        var migrated = 0;
        var taxRate = state.Policy.HouseholdTaxRate;
        var taxPush = taxRate >= 0.28m;

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
            var taxMotivated = taxPush && cohort.Profile.MigrationPreference >= 0.65m;
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
                var splitId = CohortId.New();
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
public sealed class ReconcileStep : IEconomyStep
{
    public string Name => "16_Reconcile";

    public EconomyState Execute(EconomyState current)
    {
        InvariantChecker.AssertAll(current);
        return current;
    }
}
