using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Transactions;

namespace Novolis.Economy.Accounting;

/// <summary>
/// Read-only financial projection for an arbitrary economic scope.
/// Entity books are derived from Core authority and never posted back.
/// </summary>
public sealed record FinancialProjection(
  FinancialScope Scope,
  IReadOnlyList<ProjectedBalanceSheet> EntityBooks,
  Money TotalAssets,
  Money TotalLiabilities,
  Money NetWorth,
  Money Cash,
  Money DepositsHeld,
  Money LoansReceivable,
  Money LoansPayable,
  Money ObligationsReceivable,
  Money ObligationsPayable,
  Money UndrawnCommittedCredit,
  decimal UnpricedHoldings,
  Money MonetaryPositionChange,
  IReadOnlyList<EconomicTransaction> Transactions)
{
  /// <summary>Whether the projected scope has no unpriced holdings.</summary>
  public bool IsFullyValued => UnpricedHoldings == 0m;

  /// <summary>Whether the projected balance sheet closes.</summary>
  public bool IsBalanced =>
    TotalAssets.Amount - TotalLiabilities.Amount - NetWorth.Amount == 0m;
}
