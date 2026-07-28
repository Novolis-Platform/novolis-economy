using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Accounting.Extensions;
using TUnit.Core;

namespace Novolis.Economy.Unit;

public sealed class AccountingExtensionsTests
{
    [Test]
    public async Task SeedCash_IsTrialBalanced_AndBalanceSheetMatches()
    {
        var firm = FirmId.From(Guid.Parse("a1000000-0000-4000-8000-000000000001"));
        var ledger = new FirmLedger(firm);
        ledger.SeedCash(Money.From(100m), SimulationDate.Epoch);

        await Assert.That(ledger.IsTrialBalanced()).IsTrue();
        var bs = ledger.BalanceSheet();
        await Assert.That(bs.Cash.Amount).IsEqualTo(100m);
        await Assert.That(bs.Equity.Amount).IsEqualTo(100m);
        await Assert.That(bs.TotalAssets.Amount).IsEqualTo(bs.TotalLiabilitiesAndEquity.Amount);
    }

    [Test]
    public async Task CashSale_ReportsPositiveRevenueAndCogs()
    {
        var firm = FirmId.From(Guid.Parse("a1000000-0000-4000-8000-000000000002"));
        var ledger = new FirmLedger(firm);
        ledger.SeedCash(Money.From(50m), SimulationDate.Epoch);
        ledger.SeedInventory(Money.From(20m), SimulationDate.Epoch);
        LedgerEngine.PostCashSale(ledger, revenue: Money.From(30m), cogs: Money.From(10m), SimulationDate.Epoch);

        var pl = ledger.IncomeStatement();
        await Assert.That(pl.Revenue.Amount).IsEqualTo(30m);
        await Assert.That(pl.CostOfGoodsSold.Amount).IsEqualTo(10m);
        await Assert.That(pl.NetIncome.Amount).IsEqualTo(20m);
        await Assert.That(ledger.IsTrialBalanced()).IsTrue();

        var insight = ledger.ToInsight();
        await Assert.That(insight.Revenue.Amount).IsEqualTo(30m);
        await Assert.That(insight.Cash.Amount).IsEqualTo(80m);
    }

    [Test]
    public async Task LoanDisbursement_NotesReceivableAndPayable()
    {
        var lenderId = FirmId.From(Guid.Parse("a1000000-0000-4000-8000-000000000003"));
        var borrowerId = FirmId.From(Guid.Parse("a1000000-0000-4000-8000-000000000004"));
        var lender = new FirmLedger(lenderId);
        var borrower = new FirmLedger(borrowerId);
        lender.SeedCash(Money.From(200m), SimulationDate.Epoch);
        borrower.SeedCash(Money.From(10m), SimulationDate.Epoch);

        LedgerEngine.PostLoanDisbursement(lender, borrower, Money.From(40m), SimulationDate.Epoch);

        await Assert.That(lender.ToInsight().NotesReceivable.Amount).IsEqualTo(40m);
        await Assert.That(borrower.ToInsight().NotesPayableOwed.Amount).IsEqualTo(40m);
        await Assert.That(borrower.Cash.Amount).IsEqualTo(50m);
        await Assert.That(lender.IsTrialBalanced()).IsTrue();
        await Assert.That(borrower.IsTrialBalanced()).IsTrue();
    }

    [Test]
    public async Task LedgerBookSnapshot_ReportsInvoiceArAndLedgerArSeparately()
    {
        var firm = FirmId.From(Guid.Parse("a1000000-0000-4000-8000-000000000005"));
        var ledger = new FirmLedger(firm);
        ledger.SeedCash(Money.From(10m), SimulationDate.Epoch);
        ledger.Post(AccountRole.AccountsReceivable, AccountRole.Revenue, Money.From(15m), SimulationDate.Epoch, "book AR");

        var invoices = new List<Invoice>
        {
            new(Guid.Parse("b1000000-0000-4000-8000-000000000001"), firm, null, Money.From(25m), SimulationHour.Epoch)
        };

        var snap = new Dictionary<FirmId, FirmLedger> { [firm] = ledger }.Snapshot(invoices);
        await Assert.That(snap.LedgerAccountsReceivable.Amount).IsEqualTo(15m);
        await Assert.That(snap.InvoiceOpenReceivables.Amount).IsEqualTo(25m);
        await Assert.That(snap.OpenInvoiceCount).IsEqualTo(1);
        await Assert.That(snap.OpsTotalCash.Amount).IsEqualTo(10m);
    }
}
