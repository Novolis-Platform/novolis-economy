using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Accounting;

/// <summary>Double-entry posting helpers for commerce flows.</summary>
public static class LedgerEngine
{
  /// <summary>Records purchase of inventory for cash.</summary>
  public static void PostCashPurchase(FirmLedger ledger, Money amount, SimulationDate date) =>
    ledger.Post(AccountRole.Inventory, AccountRole.Cash, amount, date, "Cash purchase");

  /// <summary>Records sale: debit cash, credit revenue; debit COGS, credit inventory.</summary>
  public static void PostCashSale(
    FirmLedger ledger,
    Money revenue,
    Money cogs,
    SimulationDate date)
  {
    ledger.Post(AccountRole.Cash, AccountRole.Revenue, revenue, date, "Cash sale");
    if (cogs.Amount > 0m)
    {
      ledger.Post(AccountRole.CostOfGoodsSold, AccountRole.Inventory, cogs, date, "COGS");
    }
  }

  /// <summary>Accrues wages.</summary>
  public static void AccrueWages(FirmLedger ledger, Money amount, SimulationDate date) =>
    ledger.Post(AccountRole.WageExpense, AccountRole.WagesPayable, amount, date, "Wage accrual");

  /// <summary>Pays accrued wages from cash.</summary>
  public static void PayWages(FirmLedger ledger, Money amount, SimulationDate date) =>
    ledger.Post(AccountRole.WagesPayable, AccountRole.Cash, amount, date, "Wage payment");

  /// <summary>Writes off spoiled inventory to COGS.</summary>
  public static void WriteOffInventory(FirmLedger ledger, Money amount, SimulationDate date) =>
    ledger.Post(AccountRole.CostOfGoodsSold, AccountRole.Inventory, amount, date, "Spoilage");

  /// <summary>Writes off onboard fuel burned to transport fuel expense.</summary>
  public static void PostFuelBurn(FirmLedger ledger, Money amount, SimulationDate date) =>
    ledger.Post(AccountRole.TransportFuelExpense, AccountRole.Inventory, amount, date, "Transport fuel burn");

  /// <summary>Pays a corridor toll from cash when affordable.</summary>
  public static bool TryPostToll(FirmLedger ledger, Money amount, SimulationDate date)
  {
    if (amount.Amount <= 0m)
    {
      return true;
    }

    if (ledger.Cash.Amount + 0.0000001m < amount.Amount)
    {
      return false;
    }

    ledger.Post(AccountRole.TransportTollExpense, AccountRole.Cash, amount, date, "Transport toll");
    return true;
  }

  /// <summary>Lender: notes receivable ↑ / cash ↓. Borrower: cash ↑ / notes payable ↑.</summary>
  public static void PostLoanDisbursement(
    FirmLedger lender,
    FirmLedger borrower,
    Money principal,
    SimulationDate date)
  {
    lender.Post(AccountRole.NotesReceivable, AccountRole.Cash, principal, date, "Loan disbursement");
    borrower.Post(AccountRole.Cash, AccountRole.NotesPayable, principal, date, "Loan proceeds");
  }

  /// <summary>
  /// Household lender: notes receivable ↑ / equity ↑ (budget already debited).
  /// Borrower: cash ↑ / notes payable ↑.
  /// </summary>
  public static void PostHouseholdLoanDisbursement(
    FirmLedger lender,
    FirmLedger borrower,
    Money principal,
    SimulationDate date)
  {
    lender.Post(AccountRole.NotesReceivable, AccountRole.Equity, principal, date, "Household loan disbursement");
    borrower.Post(AccountRole.Cash, AccountRole.NotesPayable, principal, date, "Loan proceeds");
  }

  /// <summary>
  /// Borrower pays household lender: notes ↓; cash leaves borrower (budget credited separately).
  /// </summary>
  public static void PostHouseholdLoanRepayment(
    FirmLedger lender,
    FirmLedger borrower,
    Money amount,
    SimulationDate date)
  {
    borrower.Post(AccountRole.NotesPayable, AccountRole.Cash, amount, date, "Loan repayment");
    lender.Post(AccountRole.Equity, AccountRole.NotesReceivable, amount, date, "Household loan repayment");
  }

  /// <summary>Accrue interest onto notes without moving cash.</summary>
  public static void PostInterestAccrual(
    FirmLedger lender,
    FirmLedger borrower,
    Money interest,
    SimulationDate date)
  {
    lender.Post(AccountRole.NotesReceivable, AccountRole.InterestIncome, interest, date, "Interest accrual");
    borrower.Post(AccountRole.InterestExpense, AccountRole.NotesPayable, interest, date, "Interest accrual");
  }

  /// <summary>Borrower pays lender; reduces notes on both sides.</summary>
  public static void PostLoanRepayment(
    FirmLedger lender,
    FirmLedger borrower,
    Money amount,
    SimulationDate date)
  {
    borrower.Post(AccountRole.NotesPayable, AccountRole.Cash, amount, date, "Loan repayment");
    lender.Post(AccountRole.Cash, AccountRole.NotesReceivable, amount, date, "Loan repayment");
  }
}
