using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>
/// Minimal posting capability used by finance mechanics. The accounting
/// package may adapt a ledger to this contract; Finance does not depend on it.
/// </summary>
public interface ILoanLedger<TDate>
{
  /// <summary>Current liquid balance available to fund a loan.</summary>
  Money Cash { get; }

  /// <summary>Posts a commercial loan disbursement.</summary>
  void PostLoanDisbursement(
    ILoanLedger<TDate> borrower,
    Money principal,
    TDate date);

  /// <summary>Posts a household-funded loan disbursement.</summary>
  void PostHouseholdLoanDisbursement(
    ILoanLedger<TDate> borrower,
    Money principal,
    TDate date);

  /// <summary>Posts interest accrual on a loan.</summary>
  void PostInterestAccrual(
    ILoanLedger<TDate> borrower,
    Money interest,
    TDate date);

  /// <summary>Posts a commercial loan repayment.</summary>
  void PostLoanRepayment(
    ILoanLedger<TDate> borrower,
    Money amount,
    TDate date);

  /// <summary>Posts repayment to a household-funded loan.</summary>
  void PostHouseholdLoanRepayment(
    ILoanLedger<TDate> borrower,
    Money amount,
    TDate date);
}
