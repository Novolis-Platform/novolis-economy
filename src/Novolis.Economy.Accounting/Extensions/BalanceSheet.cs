namespace Novolis.Economy.Accounting.Extensions;

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
