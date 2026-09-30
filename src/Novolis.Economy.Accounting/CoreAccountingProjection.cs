using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;

namespace Novolis.Economy.Accounting;

/// <summary>
/// Projects Core's authoritative monetary position into accounting reports.
/// Journal entries remain useful evidence, but they do not create a second
/// economic cash authority.
/// </summary>
public static class CoreAccountingProjection
{
  /// <summary>Build a cash reconciliation for one firm.</summary>
  public static CashProjection ProjectCash(
    EconomyState state,
    FirmId firmId,
    FirmLedger? ledger = null)
  {
    var coreCash = CashLedger.Balance(state, firmId.AsCore());
    var accountingCash = ledger?.Cash ?? coreCash;
    return new CashProjection(firmId, coreCash, accountingCash);
  }

  /// <summary>Throw when an accounting cash projection has drifted.</summary>
  public static void AssertReconciled(CashProjection projection)
  {
    ArgumentNullException.ThrowIfNull(projection);
    if (!projection.IsReconciled)
    {
      throw new InvalidOperationException(
        $"Accounting cash for {projection.FirmId} differs from Core by {projection.Difference}.");
    }
  }
}
