using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;

namespace Novolis.Economy.Accounting;

/// <summary>Reconciliation result between Core money and an accounting view.</summary>
public sealed record CashProjection(
  FirmId FirmId,
  Money CoreCash,
  Money AccountingCash)
{
  /// <summary>Difference between the accounting projection and Core authority.</summary>
  public Money Difference => AccountingCash - CoreCash;

  /// <summary>True when the two views agree within decimal precision.</summary>
  public bool IsReconciled => Difference.Amount == 0m;
}
