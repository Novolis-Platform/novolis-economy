namespace Novolis.Economy.Core.Extensions;

/// <summary>
/// Read-only projected balance sheet for one legal entity.
/// Derived from Core claim tables — not a mutable chart of accounts.
/// Undrawn committed credit is capacity only (not an asset).
/// </summary>
public sealed record ProjectedBalanceSheet(
    LegalEntityId Id,
    LegalEntityKind Kind,
    Money Cash,
    Money DepositsHeld,
    Money LoansReceivable,
    Money ObligationsReceivable,
    Money HoldingsValued,
    decimal HoldingsUnpricedQuantity,
    Money DepositLiabilities,
    Money LoansPayable,
    Money ObligationsPayable,
    Money UndrawnCommittedCredit,
    Money TotalAssets,
    Money TotalLiabilities,
    Money NetWorth);

/// <summary>
/// Economy-wide period appropriation from <see cref="PeriodFlowLedger"/>.
/// ProductionOutputValue is a flow counter (often quantity-as-money), not mercantile revenue.
/// </summary>
public sealed record ProjectedPeriodIncome(
    Money MoneyCreated,
    Money MoneyDestroyed,
    Money NetMoneyCreated,
    Money WagesAccrued,
    Money TaxCollected,
    Money TransfersPaid,
    Money ObligationsPaid,
    Money ProductionOutputValue);

/// <summary>Sectoral stock aggregate for one institutional kind.</summary>
public sealed record SectoralBooksRow(
    LegalEntityKind Kind,
    int EntityCount,
    Money Cash,
    Money DepositsHeld,
    Money DepositLiabilities,
    Money LoansReceivable,
    Money LoansPayable,
    Money ObligationsReceivable,
    Money ObligationsPayable,
    Money HoldingsValued,
    decimal HoldingsUnpricedQuantity,
    Money NetWorth);

/// <summary>Full projected accounts snapshot for reporting (Core-only).</summary>
public sealed record ProjectedAccountsSnapshot(
    IReadOnlyList<SectoralBooksRow> Sectors,
    IReadOnlyList<ProjectedBalanceSheet> Entities,
    ProjectedPeriodIncome LastPeriod,
    Money AggregateNetWorth,
    decimal AggregateHoldingsUnpricedQuantity);
