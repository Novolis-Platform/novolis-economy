using System.Collections.Immutable;

namespace Novolis.Economy.Core;

/// <summary>Period flow counters for SFC-style reconcile (SPEC §18).</summary>
public sealed record PeriodFlowLedger(
    Money MoneyCreated,
    Money MoneyDestroyed,
    Money CashMoved,
    Money ObligationsPaid,
    Money TaxCollected,
    Money TransfersPaid,
    Money ProductionOutputValue,
    Money WagesAccrued,
    IReadOnlyDictionary<EconomicAssetId, decimal>? ProductionOutputQuantity = null,
    IReadOnlyDictionary<EconomicAssetId, decimal>? ConsumedQuantity = null)
{
    /// <summary>Empty ledger.</summary>
    public static PeriodFlowLedger Empty { get; } = new(
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero,
        Money.Zero,
        ImmutableDictionary<EconomicAssetId, decimal>.Empty,
        ImmutableDictionary<EconomicAssetId, decimal>.Empty);

    public PeriodFlowLedger RecordMoneyCreated(Money m) => this with { MoneyCreated = MoneyCreated + m };
    public PeriodFlowLedger RecordMoneyDestroyed(Money m) => this with { MoneyDestroyed = MoneyDestroyed + m };
    public PeriodFlowLedger RecordCashMoved(Money m) => this with { CashMoved = CashMoved + m };
    public PeriodFlowLedger RecordObligationPaid(Money m) => this with { ObligationsPaid = ObligationsPaid + m };
    public PeriodFlowLedger RecordTax(Money m) => this with { TaxCollected = TaxCollected + m };
    public PeriodFlowLedger RecordTransfer(Money m) => this with { TransfersPaid = TransfersPaid + m };
    public PeriodFlowLedger RecordWages(Money m) => this with { WagesAccrued = WagesAccrued + m };

    /// <summary>Record physical production without assigning a monetary value.</summary>
    public PeriodFlowLedger RecordProductionQuantity(EconomicAssetId asset, decimal quantity)
    {
        if (quantity == 0m)
            return this;
        if (quantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var quantities = new Dictionary<EconomicAssetId, decimal>(
            ProductionOutputQuantity ??
            ImmutableDictionary<EconomicAssetId, decimal>.Empty);
        quantities[asset] = quantities.GetValueOrDefault(asset) + quantity;
        return this with { ProductionOutputQuantity = quantities };
    }

    /// <summary>Record physical consumption without assigning a monetary value.</summary>
    public PeriodFlowLedger RecordConsumedQuantity(EconomicAssetId asset, decimal quantity)
    {
        if (quantity == 0m)
            return this;
        if (quantity < 0m)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var quantities = new Dictionary<EconomicAssetId, decimal>(
            ConsumedQuantity ??
            ImmutableDictionary<EconomicAssetId, decimal>.Empty);
        quantities[asset] = quantities.GetValueOrDefault(asset) + quantity;
        return this with { ConsumedQuantity = quantities };
    }

    /// <summary>
    /// Record a monetary production valuation explicitly. Raw physical
    /// quantities must never call this method directly.
    /// </summary>
    public PeriodFlowLedger RecordProductionValue(Money value) =>
        this with { ProductionOutputValue = ProductionOutputValue + value };

    /// <summary>Physical production quantities, empty for legacy snapshots.</summary>
    public IReadOnlyDictionary<EconomicAssetId, decimal> ProductionQuantities =>
        ProductionOutputQuantity ?? ImmutableDictionary<EconomicAssetId, decimal>.Empty;

    /// <summary>Physical consumption quantities, empty for legacy snapshots.</summary>
    public IReadOnlyDictionary<EconomicAssetId, decimal> ConsumptionQuantities =>
        ConsumedQuantity ?? ImmutableDictionary<EconomicAssetId, decimal>.Empty;

    /// <summary>Net endogenous money creation this period.</summary>
    public Money NetMoneyCreated => MoneyCreated - MoneyDestroyed;
}
