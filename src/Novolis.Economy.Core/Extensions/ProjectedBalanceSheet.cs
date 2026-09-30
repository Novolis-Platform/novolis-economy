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