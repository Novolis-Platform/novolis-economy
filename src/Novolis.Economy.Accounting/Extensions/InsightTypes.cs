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

/// <summary>One trial-balance row (storage / signed ledger balance).</summary>
public sealed record TrialBalanceLine(AccountRole Role, Money StorageBalance);

/// <summary>Income statement in presentation amounts (revenue positive, expenses positive).</summary>
public sealed record IncomeStatement(
    Money Revenue,
    Money InterestIncome,
    Money CostOfGoodsSold,
    Money WageExpense,
    Money TransportFuelExpense,
    Money TransportTollExpense,
    Money InterestExpense,
    Money NetIncome);

/// <summary>Balance sheet in presentation amounts (assets, liabilities owed, equity positive).</summary>
public sealed record BalanceSheet(
    Money Cash,
    Money Inventory,
    Money AccountsReceivable,
    Money NotesReceivable,
    Money TotalAssets,
    Money AccountsPayable,
    Money WagesPayable,
    Money NotesPayable,
    Money TotalLiabilities,
    Money Equity,
    Money TotalLiabilitiesAndEquity);

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
