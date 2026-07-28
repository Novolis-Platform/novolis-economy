namespace Novolis.Economy.Accounting.Extensions;

/// <summary>Aggregate ops ledger book insights.</summary>
public static class LedgerBookExtensions
{
    /// <summary>
    /// Snapshot over firm ledgers. When <paramref name="invoices"/> is provided,
    /// reports invoice open receivables separately from ledger AR.
    /// </summary>
    public static LedgerBookSnapshot Snapshot(
        this IReadOnlyDictionary<FirmId, FirmLedger> ledgers,
        IEnumerable<Invoice>? invoices = null)
    {
        var firms = ledgers.Values
            .OrderBy(l => l.FirmId.Value)
            .Select(l => l.ToInsight())
            .ToList();

        var opsCash = Money.From(firms.Sum(f => f.Cash.Amount));
        var ledgerAr = Money.From(firms.Sum(f => f.AccountsReceivable.Amount));

        var invoiceList = invoices?.ToList() ?? [];
        var open = invoiceList.Where(i => !i.IsSettled).ToList();
        var invoiceAr = Money.From(open.Sum(i => i.Remaining.Amount));
        var settled = invoiceList.Count - open.Count;

        return new LedgerBookSnapshot(
            FirmCount: firms.Count,
            OpsTotalCash: opsCash,
            InvoiceOpenReceivables: invoiceAr,
            LedgerAccountsReceivable: ledgerAr,
            OpenInvoiceCount: open.Count,
            SettledInvoiceCount: settled,
            Firms: firms);
    }
}
