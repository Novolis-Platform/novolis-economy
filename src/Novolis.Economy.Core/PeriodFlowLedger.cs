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
    Money WagesAccrued)
{
    /// <summary>Empty ledger.</summary>
    public static PeriodFlowLedger Empty { get; } = new(
        Money.Zero, Money.Zero, Money.Zero, Money.Zero, Money.Zero, Money.Zero, Money.Zero, Money.Zero);

    public PeriodFlowLedger RecordMoneyCreated(Money m) => this with { MoneyCreated = MoneyCreated + m };
    public PeriodFlowLedger RecordMoneyDestroyed(Money m) => this with { MoneyDestroyed = MoneyDestroyed + m };
    public PeriodFlowLedger RecordCashMoved(Money m) => this with { CashMoved = CashMoved + m };
    public PeriodFlowLedger RecordObligationPaid(Money m) => this with { ObligationsPaid = ObligationsPaid + m };
    public PeriodFlowLedger RecordTax(Money m) => this with { TaxCollected = TaxCollected + m };
    public PeriodFlowLedger RecordTransfer(Money m) => this with { TransfersPaid = TransfersPaid + m };
    public PeriodFlowLedger RecordProduction(Money m) => this with { ProductionOutputValue = ProductionOutputValue + m };
    public PeriodFlowLedger RecordWages(Money m) => this with { WagesAccrued = WagesAccrued + m };

    /// <summary>Net endogenous money creation this period.</summary>
    public Money NetMoneyCreated => MoneyCreated - MoneyDestroyed;
}
