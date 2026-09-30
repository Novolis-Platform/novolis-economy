namespace Novolis.Economy.Core.Extensions;

/// <summary>
/// Economy-wide period appropriation from <see cref="PeriodFlowLedger"/>.
/// Production output is physical by asset; a monetary value is available only
/// after an explicit valuation.
/// </summary>
public sealed record ProjectedPeriodIncome(
  Money MoneyCreated,
  Money MoneyDestroyed,
  Money NetMoneyCreated,
  Money WagesAccrued,
  Money TaxCollected,
  Money TransfersPaid,
  Money ObligationsPaid,
  Money ProductionOutputValue,
  IReadOnlyDictionary<EconomicAssetId, decimal>? ProductionOutputQuantity = null)
{
  /// <summary>Physical production quantities, empty for legacy snapshots.</summary>
  public IReadOnlyDictionary<EconomicAssetId, decimal> PhysicalProduction =>
    ProductionOutputQuantity ??
    new Dictionary<EconomicAssetId, decimal>();
}