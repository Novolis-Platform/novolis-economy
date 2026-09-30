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