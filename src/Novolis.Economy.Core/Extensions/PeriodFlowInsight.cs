namespace Novolis.Economy.Core.Extensions;

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
  Money WagesAccrued,
  IReadOnlyDictionary<EconomicAssetId, decimal>? ProductionOutputQuantity = null,
  IReadOnlyDictionary<EconomicAssetId, decimal>? ConsumedQuantity = null);