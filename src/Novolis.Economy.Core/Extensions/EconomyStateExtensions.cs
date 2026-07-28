using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Labor;

namespace Novolis.Economy.Core.Extensions;

/// <summary>High-level statistics and insights over <see cref="EconomyState"/>.</summary>
public static class EconomyStateExtensions
{
    /// <summary>Sum of all entity vault cash.</summary>
    public static Money TotalCash(this EconomyState state) =>
        Money.From(state.Entities.Values.Sum(e => e.Cash.Amount));

    /// <summary>Sum of all deposit balances (inside money).</summary>
    public static Money TotalDeposits(this EconomyState state) =>
        Money.From(state.Deposits.Sum(d => d.Balance.Amount));

    /// <summary>Vault cash + deposits (not a unique medium of exchange — useful as a stock pulse).</summary>
    public static Money BroadMoney(this EconomyState state) =>
        state.TotalCash() + state.TotalDeposits();

    /// <summary>Principal on performing and delinquent loans.</summary>
    public static Money LoanPrincipalOutstanding(this EconomyState state) =>
        Money.From(
            state.Loans.Values
                .Where(l => l.Status is LoanStatus.Performing or LoanStatus.Delinquent)
                .Sum(l => l.PrincipalOutstanding.Amount));

    /// <summary>Undrawn capacity on committed facilities only.</summary>
    public static Money UndrawnCommittedCredit(this EconomyState state) =>
        Money.From(
            state.CreditFacilities.Values
                .Where(f => f.IsCommitted)
                .Sum(f => f.Available.Amount));

    /// <summary>Total households across cohorts.</summary>
    public static int TotalHouseholds(this EconomyState state) =>
        state.Cohorts.Values.Sum(c => c.HouseholdCount);

    /// <summary>Count entities by institutional kind.</summary>
    public static IReadOnlyDictionary<LegalEntityKind, int> CountEntitiesByKind(this EconomyState state) =>
        state.Entities.Values
            .GroupBy(e => e.Kind)
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>Sum vault cash by institutional kind (sectoral cash stocks).</summary>
    public static IReadOnlyDictionary<LegalEntityKind, Money> CashByKind(this EconomyState state) =>
        state.Entities.Values
            .GroupBy(e => e.Kind)
            .ToDictionary(g => g.Key, g => Money.From(g.Sum(e => e.Cash.Amount)));

    /// <summary>Entities whose liquidity surplus is negative (due-now exceeds accessible means).</summary>
    public static IReadOnlyList<LegalEntityId> IlliquidEntities(this EconomyState state) =>
        state.Entities.Keys
            .Where(id => Liquidity.Of(state, id).Surplus.Amount < 0m)
            .ToList();

    /// <summary>Entities with simple solvency &lt; 0 (cash+deposits+undrawn − loans).</summary>
    public static IReadOnlyList<LegalEntityId> InsolventHintEntities(this EconomyState state) =>
        state.Entities.Keys
            .Where(id => Liquidity.SimpleSolvency(state, id).Amount < 0m)
            .ToList();

    /// <summary>Minsky-style stress: illiquid but not insolvent on the simple book measure.</summary>
    public static IReadOnlyList<LegalEntityId> IlliquidButSolventEntities(this EconomyState state) =>
        state.Entities.Keys
            .Where(id =>
            {
                var liq = Liquidity.Of(state, id);
                var sol = Liquidity.SimpleSolvency(state, id);
                return liq.Surplus.Amount < 0m && sol.Amount >= 0m;
            })
            .ToList();

    /// <summary>Pending obligations due at or before the current period.</summary>
    public static IReadOnlyList<PaymentObligation> DueObligations(this EconomyState state) =>
        state.Obligations
            .Where(o => o.Status == ObligationStatus.Pending && o.DuePeriod <= state.Period)
            .ToList();

    /// <summary>Soft invariant report (does not throw).</summary>
    public static IReadOnlyList<InvariantViolation> CheckInvariants(this EconomyState state) =>
        InvariantChecker.Check(state);

    /// <summary>Compact macro snapshot for logging / UI.</summary>
    public static EconomySnapshot Snapshot(this EconomyState state)
    {
        var loans = state.Loans.Values.ToList();
        var obligations = state.Obligations;
        return new EconomySnapshot(
            Period: state.Period,
            EntityCount: state.Entities.Count,
            RegionCount: state.Regions.Count,
            CohortCount: state.Cohorts.Count,
            HouseholdCount: state.TotalHouseholds(),
            ActivityCount: state.Activities.Count,
            HoldingSlots: state.Holdings.Count,
            InFlightTransfers: state.Transfers.Count,
            PerformingLoans: loans.Count(l => l.Status == LoanStatus.Performing),
            DelinquentLoans: loans.Count(l => l.Status == LoanStatus.Delinquent),
            DefaultedLoans: loans.Count(l => l.Status == LoanStatus.Defaulted),
            PendingObligations: obligations.Count(o => o.Status == ObligationStatus.Pending),
            DelinquentObligations: obligations.Count(o => o.Status == ObligationStatus.Delinquent),
            TotalCash: state.TotalCash(),
            TotalDeposits: state.TotalDeposits(),
            BroadMoney: state.BroadMoney(),
            LoanPrincipalOutstanding: state.LoanPrincipalOutstanding(),
            UndrawnCommittedCredit: state.UndrawnCommittedCredit(),
            NetMoneyCreatedThisPeriod: state.Flows.NetMoneyCreated,
            EntitiesByKind: state.CountEntitiesByKind(),
            CashByKind: state.CashByKind());
    }

    /// <summary>Financial insight for every entity.</summary>
    public static IReadOnlyList<EntityFinancialInsight> EntityInsights(this EconomyState state) =>
        state.Entities.Values.Select(e => state.InsightFor(e.Id)).ToList();

    /// <summary>Financial insight for one entity.</summary>
    public static EntityFinancialInsight InsightFor(this EconomyState state, LegalEntityId id)
    {
        if (!state.Entities.TryGetValue(id, out var entity))
            throw new InvalidOperationException($"Unknown entity {id}.");

        var liq = Liquidity.Of(state, id);
        var solvency = Liquidity.SimpleSolvency(state, id);
        var asBorrower = Money.From(
            state.Loans.Values
                .Where(l => l.Borrower.Equals(id) && l.Status is LoanStatus.Performing or LoanStatus.Delinquent)
                .Sum(l => l.PrincipalOutstanding.Amount));
        var asLender = Money.From(
            state.Loans.Values
                .Where(l => l.Lender.Equals(id) && l.Status is LoanStatus.Performing or LoanStatus.Delinquent)
                .Sum(l => l.PrincipalOutstanding.Amount));
        var receivable = Money.From(
            state.Obligations
                .Where(o => o.Creditor.Equals(id) && o.Status == ObligationStatus.Pending)
                .Sum(o => o.Amount.Amount));

        return new EntityFinancialInsight(
            Id: id,
            Kind: entity.Kind,
            Cash: entity.Cash,
            Deposits: DepositLedger.TotalFor(state, id),
            LoansAsBorrower: asBorrower,
            LoansAsLender: asLender,
            UndrawnCommittedCredit: liq.UndrawnCommittedCredit,
            PendingObligationsDue: liq.DueNow,
            PendingObligationsReceivable: receivable,
            Liquidity: liq,
            SimpleSolvency: solvency,
            IsIlliquid: liq.Surplus.Amount < 0m,
            IsInsolventHint: solvency.Amount < 0m);
    }

    /// <summary>Region insights for all regions.</summary>
    public static IReadOnlyList<RegionInsight> RegionInsights(this EconomyState state) =>
        state.Regions.Values.Select(r => state.InsightFor(r.Id)).ToList();

    /// <summary>Capacity / labor insight for one region.</summary>
    public static RegionInsight InsightFor(this EconomyState state, RegionId regionId)
    {
        if (!state.Regions.TryGetValue(regionId, out var region))
            throw new InvalidOperationException($"Unknown region {regionId}.");

        var households = RegionCapacity.OccupiedLiving(state, regionId);
        var remainingLiving = RegionCapacity.RemainingLiving(state, region);
        var installed = RegionCapacity.InstalledProductionSpace(state, regionId);
        var remainingProd = RegionCapacity.RemainingProduction(state, region);
        var logisticsLoad = RegionCapacity.LogisticsLoad(state, regionId);
        var remainingLog = RegionCapacity.RemainingLogistics(state, region);
        var labor = LaborSupply.Calculate(state, regionId);
        var activities = state.Activities.Values.Count(a => a.RegionId.Equals(regionId));
        var holdings = state.Holdings.Values
            .Where(h => h.RegionId.Equals(regionId))
            .Sum(h => h.Quantity);

        return new RegionInsight(
            Id: regionId,
            LivingCapacity: region.LivingCapacity,
            Households: households,
            RemainingLiving: remainingLiving,
            LivingUtilization: Utilization(households, region.LivingCapacity),
            ProductionCapacity: region.ProductionCapacity,
            InstalledProductionSpace: installed,
            RemainingProduction: remainingProd,
            ProductionUtilization: Utilization(installed, region.ProductionCapacity),
            LogisticsCapacity: region.LogisticsCapacity,
            LogisticsLoad: logisticsLoad,
            RemainingLogistics: remainingLog,
            LogisticsUtilization: Utilization(logisticsLoad, region.LogisticsCapacity),
            LaborSupplyHours: labor,
            ActivityCount: activities,
            HoldingQuantity: holdings);
    }

    /// <summary>Cohort insights for all cohorts.</summary>
    public static IReadOnlyList<CohortInsight> CohortInsights(this EconomyState state) =>
        state.Cohorts.Values.Select(c => c.ToInsight()).ToList();

    /// <summary>Last-period flow ledger as a structured insight.</summary>
    public static PeriodFlowInsight FlowInsight(this EconomyState state)
    {
        var f = state.Flows;
        return new PeriodFlowInsight(
            f.MoneyCreated,
            f.MoneyDestroyed,
            f.NetMoneyCreated,
            f.CashMoved,
            f.ObligationsPaid,
            f.TaxCollected,
            f.TransfersPaid,
            f.ProductionOutputValue,
            f.WagesAccrued);
    }

    /// <summary>Obligation book: counts, sums, due-now, pending by kind.</summary>
    public static ObligationBookInsight ObligationBook(this EconomyState state)
    {
        var obs = state.Obligations;
        Money SumStatus(ObligationStatus s) =>
            Money.From(obs.Where(o => o.Status == s).Sum(o => o.Amount.Amount));

        var pendingByKind = obs
            .Where(o => o.Status == ObligationStatus.Pending)
            .GroupBy(o => o.Kind)
            .ToDictionary(g => g.Key, g => Money.From(g.Sum(o => o.Amount.Amount)));

        var dueNow = Money.From(
            obs.Where(o => o.Status == ObligationStatus.Pending && o.DuePeriod <= state.Period)
                .Sum(o => o.Amount.Amount));

        return new ObligationBookInsight(
            PendingCount: obs.Count(o => o.Status == ObligationStatus.Pending),
            DelinquentCount: obs.Count(o => o.Status == ObligationStatus.Delinquent),
            DefaultedCount: obs.Count(o => o.Status == ObligationStatus.Defaulted),
            PaidCount: obs.Count(o => o.Status == ObligationStatus.Paid),
            PendingSum: SumStatus(ObligationStatus.Pending),
            DelinquentSum: SumStatus(ObligationStatus.Delinquent),
            DueNow: dueNow,
            PendingSumByKind: pendingByKind);
    }

    /// <summary>Credit facilities + loan status book.</summary>
    public static CreditBookInsight CreditBook(this EconomyState state)
    {
        var facilities = state.CreditFacilities.Values.ToList();
        var loans = state.Loans.Values.ToList();
        return new CreditBookInsight(
            FacilityCount: facilities.Count,
            FacilityLimitTotal: Money.From(facilities.Sum(f => f.Limit.Amount)),
            FacilityDrawnTotal: Money.From(facilities.Sum(f => f.Drawn.Amount)),
            UndrawnCommitted: state.UndrawnCommittedCredit(),
            PerformingLoans: loans.Count(l => l.Status == LoanStatus.Performing),
            DelinquentLoans: loans.Count(l => l.Status == LoanStatus.Delinquent),
            DefaultedLoans: loans.Count(l => l.Status == LoanStatus.Defaulted),
            LoanPrincipalOutstanding: state.LoanPrincipalOutstanding());
    }

    private static decimal Utilization(decimal used, decimal capacity) =>
        capacity <= 0m ? (used > 0m ? 1m : 0m) : Math.Clamp(used / capacity, 0m, decimal.MaxValue);
}
