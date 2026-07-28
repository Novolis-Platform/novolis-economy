namespace Novolis.Economy.Core.Extensions;

/// <summary>Macro snapshot of an <see cref="EconomyState"/> for dashboards and diagnostics.</summary>
public sealed record EconomySnapshot(
    int Period,
    int EntityCount,
    int RegionCount,
    int CohortCount,
    int HouseholdCount,
    int ActivityCount,
    int HoldingSlots,
    int InFlightTransfers,
    int PerformingLoans,
    int DelinquentLoans,
    int DefaultedLoans,
    int PendingObligations,
    int DelinquentObligations,
    Money TotalCash,
    Money TotalDeposits,
    Money BroadMoney,
    Money LoanPrincipalOutstanding,
    Money UndrawnCommittedCredit,
    Money NetMoneyCreatedThisPeriod,
    IReadOnlyDictionary<LegalEntityKind, int> EntitiesByKind,
    IReadOnlyDictionary<LegalEntityKind, Money> CashByKind);

/// <summary>Financial insight for one legal entity in context of the economy.</summary>
public sealed record EntityFinancialInsight(
    LegalEntityId Id,
    LegalEntityKind Kind,
    Money Cash,
    Money Deposits,
    Money LoansAsBorrower,
    Money LoansAsLender,
    Money UndrawnCommittedCredit,
    Money PendingObligationsDue,
    Money PendingObligationsReceivable,
    LiquidityPosition Liquidity,
    Money SimpleSolvency,
    bool IsIlliquid,
    bool IsInsolventHint);

/// <summary>Capacity and demographic insight for one region.</summary>
public sealed record RegionInsight(
    RegionId Id,
    int LivingCapacity,
    int Households,
    int RemainingLiving,
    decimal LivingUtilization,
    decimal ProductionCapacity,
    decimal InstalledProductionSpace,
    decimal RemainingProduction,
    decimal ProductionUtilization,
    decimal LogisticsCapacity,
    decimal LogisticsLoad,
    decimal RemainingLogistics,
    decimal LogisticsUtilization,
    decimal LaborSupplyHours,
    int ActivityCount,
    decimal HoldingQuantity);

/// <summary>Cohort aggregate insight.</summary>
public sealed record CohortInsight(
    CohortId Id,
    RegionId RegionId,
    int HouseholdCount,
    Money CashPerHousehold,
    Money TotalCash,
    decimal EffectiveLaborHours,
    HouseholdLaborKind LaborKind,
    decimal LaborQuality,
    LegalEntityId? HouseholdEntityId);

/// <summary>Last-period flow pulse (SPEC §18). Horizon totals belong in host runners.</summary>
public sealed record PeriodFlowInsight(
    Money MoneyCreated,
    Money MoneyDestroyed,
    Money NetMoneyCreated,
    Money CashMoved,
    Money ObligationsPaid,
    Money TaxCollected,
    Money TransfersPaid,
    Money ProductionOutputValue,
    Money WagesAccrued);

/// <summary>Obligation book by status and kind.</summary>
public sealed record ObligationBookInsight(
    int PendingCount,
    int DelinquentCount,
    int DefaultedCount,
    int PaidCount,
    Money PendingSum,
    Money DelinquentSum,
    Money DueNow,
    IReadOnlyDictionary<ObligationKind, Money> PendingSumByKind);

/// <summary>Credit facilities and loan book.</summary>
public sealed record CreditBookInsight(
    int FacilityCount,
    Money FacilityLimitTotal,
    Money FacilityDrawnTotal,
    Money UndrawnCommitted,
    int PerformingLoans,
    int DelinquentLoans,
    int DefaultedLoans,
    Money LoanPrincipalOutstanding);
