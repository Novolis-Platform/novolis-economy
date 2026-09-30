namespace Novolis.Economy.Accounting.Extensions;

/// <summary>
/// Ops ledger book snapshot. Invoice open AR and ledger AR are reported separately —
/// commercial truth vs book balance; callers must not assume they match.
/// </summary>
public sealed record LedgerBookSnapshot(
    int FirmCount,
    Money OpsTotalCash,
    Money InvoiceOpenReceivables,
    Money LedgerAccountsReceivable,
    int OpenInvoiceCount,
    int SettledInvoiceCount,
    IReadOnlyList<FirmLedgerInsight> Firms);
