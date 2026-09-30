namespace Novolis.Economy.Accounting.Extensions;

/// <summary>
/// Presentation amounts derived from a debit-positive ledger.
/// Storage: assets/expenses &gt; 0; revenue/liability/equity typically &lt; 0.
/// Presentation helpers flip credit-normal roles so revenue and liabilities owed are positive.
/// </summary>
public sealed record FirmLedgerInsight(
    FirmId FirmId,
    Money Cash,
    Money Inventory,
    Money AccountsReceivable,
    Money AccountsPayableOwed,
    Money NotesReceivable,
    Money NotesPayableOwed,
    Money Revenue,
    Money CostOfGoodsSold,
    Money WageExpense,
    Money TransportFuelExpense,
    Money TransportTollExpense,
    Money InterestIncome,
    Money InterestExpense,
    Money Equity,
    int EntryCount);
