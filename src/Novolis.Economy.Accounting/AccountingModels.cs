using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Accounting;

/// <summary>Standard firm chart-of-accounts roles.</summary>
public enum AccountRole
{
  /// <summary>Cash / bank.</summary>
  Cash = 0,
  /// <summary>Inventory asset.</summary>
  Inventory = 1,
  /// <summary>Accounts receivable.</summary>
  AccountsReceivable = 2,
  /// <summary>Accounts payable.</summary>
  AccountsPayable = 3,
  /// <summary>Sales revenue.</summary>
  Revenue = 4,
  /// <summary>Cost of goods sold.</summary>
  CostOfGoodsSold = 5,
  /// <summary>Wage expense.</summary>
  WageExpense = 6,
  /// <summary>Owner equity / retained earnings.</summary>
  Equity = 7,
  /// <summary>Wage liability accrued.</summary>
  WagesPayable = 8,
  /// <summary>Transport fuel consumed while underway.</summary>
  TransportFuelExpense = 9,
  /// <summary>Corridor / port tolls paid in cash.</summary>
  TransportTollExpense = 10,
  /// <summary>Loans receivable (asset).</summary>
  NotesReceivable = 11,
  /// <summary>Loans payable (liability).</summary>
  NotesPayable = 12,
  /// <summary>Interest earned on loans.</summary>
  InterestIncome = 13,
  /// <summary>Interest owed on loans.</summary>
  InterestExpense = 14,
}
