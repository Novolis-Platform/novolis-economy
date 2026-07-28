namespace Novolis.Economy.Accounting.Extensions;

/// <summary>Read-only firm ledger insights (debit-positive storage → presentation amounts).</summary>
public static class FirmLedgerExtensions
{
    /// <summary>Presentation insight for one firm ledger.</summary>
    public static FirmLedgerInsight ToInsight(this FirmLedger ledger) =>
        new(
            FirmId: ledger.FirmId,
            Cash: ledger.Cash,
            Inventory: ledger.Balance(AccountRole.Inventory),
            AccountsReceivable: ledger.Balance(AccountRole.AccountsReceivable),
            AccountsPayableOwed: PresentCredit(ledger.Balance(AccountRole.AccountsPayable)),
            NotesReceivable: ledger.Balance(AccountRole.NotesReceivable),
            NotesPayableOwed: PresentCredit(ledger.Balance(AccountRole.NotesPayable)),
            Revenue: PresentCredit(ledger.Balance(AccountRole.Revenue)),
            CostOfGoodsSold: ledger.Balance(AccountRole.CostOfGoodsSold),
            WageExpense: ledger.Balance(AccountRole.WageExpense),
            TransportFuelExpense: ledger.Balance(AccountRole.TransportFuelExpense),
            TransportTollExpense: ledger.Balance(AccountRole.TransportTollExpense),
            InterestIncome: PresentCredit(ledger.Balance(AccountRole.InterestIncome)),
            InterestExpense: ledger.Balance(AccountRole.InterestExpense),
            Equity: PresentCredit(ledger.Balance(AccountRole.Equity)),
            EntryCount: ledger.Entries.Count);

    /// <summary>Storage-signed balances for every account role.</summary>
    public static IReadOnlyList<TrialBalanceLine> TrialBalance(this FirmLedger ledger) =>
        Enum.GetValues<AccountRole>()
            .OrderBy(r => (int)r)
            .Select(r => new TrialBalanceLine(r, ledger.Balance(r)))
            .ToList();

    /// <summary>True when Σ storage balances ≈ 0 (double-entry closed).</summary>
    public static bool IsTrialBalanced(this FirmLedger ledger, decimal tolerance = 1e-6m) =>
        Math.Abs(ledger.TrialBalance().Sum(l => l.StorageBalance.Amount)) <= tolerance;

    /// <summary>P&amp;L in presentation amounts.</summary>
    public static IncomeStatement IncomeStatement(this FirmLedger ledger)
    {
        var revenue = PresentCredit(ledger.Balance(AccountRole.Revenue));
        var interestIncome = PresentCredit(ledger.Balance(AccountRole.InterestIncome));
        var cogs = ledger.Balance(AccountRole.CostOfGoodsSold);
        var wages = ledger.Balance(AccountRole.WageExpense);
        var fuel = ledger.Balance(AccountRole.TransportFuelExpense);
        var toll = ledger.Balance(AccountRole.TransportTollExpense);
        var interestExp = ledger.Balance(AccountRole.InterestExpense);
        var net = Money.From(
            revenue.Amount + interestIncome.Amount
            - cogs.Amount - wages.Amount - fuel.Amount - toll.Amount - interestExp.Amount);
        return new IncomeStatement(revenue, interestIncome, cogs, wages, fuel, toll, interestExp, net);
    }

    /// <summary>Balance sheet in presentation amounts (unclosed books: equity includes net income).</summary>
    public static BalanceSheet BalanceSheet(this FirmLedger ledger)
    {
        var cash = ledger.Balance(AccountRole.Cash);
        var inventory = ledger.Balance(AccountRole.Inventory);
        var ar = ledger.Balance(AccountRole.AccountsReceivable);
        var notesR = ledger.Balance(AccountRole.NotesReceivable);
        var assets = Money.From(cash.Amount + inventory.Amount + ar.Amount + notesR.Amount);

        var ap = PresentCredit(ledger.Balance(AccountRole.AccountsPayable));
        var wagesPay = PresentCredit(ledger.Balance(AccountRole.WagesPayable));
        var notesP = PresentCredit(ledger.Balance(AccountRole.NotesPayable));
        var liabilities = Money.From(ap.Amount + wagesPay.Amount + notesP.Amount);

        var equity = PresentCredit(ledger.Balance(AccountRole.Equity));
        var pl = ledger.IncomeStatement();
        var equityAndRetained = Money.From(equity.Amount + pl.NetIncome.Amount);
        var liabAndEquity = Money.From(liabilities.Amount + equityAndRetained.Amount);

        return new BalanceSheet(
            cash, inventory, ar, notesR, assets,
            ap, wagesPay, notesP, liabilities,
            equityAndRetained, liabAndEquity);
    }

    /// <summary>Flip credit-normal storage (&lt; 0) to positive presentation; leave non-negative as-is.</summary>
    internal static Money PresentCredit(Money storage) =>
        storage.Amount <= 0m ? Money.From(-storage.Amount) : storage;
}
