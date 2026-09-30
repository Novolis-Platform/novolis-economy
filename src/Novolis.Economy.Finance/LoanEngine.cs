using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Finance;

/// <summary>Pure loan settlement helpers (ledger + loan mutation).</summary>
public static class LoanEngine
{
  /// <summary>
  /// Disburses a new loan when the lender has cash. Returns null on failure.
  /// </summary>
  public static Loan? TryOriginate<TLedger>(
    IDictionary<FirmId, TLedger> ledgers,
    OriginateLoan cmd,
    SimulationHour hour,
    Func<LoanId> nextId)
    where TLedger : ILoanLedger<SimulationDate>
  {
    if (cmd.Principal.Amount <= 0m
        || cmd.TermHours <= 0
        || cmd.LenderFirmId.Equals(cmd.BorrowerFirmId)
        || !ledgers.TryGetValue(cmd.LenderFirmId, out var lender)
        || !ledgers.TryGetValue(cmd.BorrowerFirmId, out var borrower)
        || lender.Cash.Amount + 0.0000001m < cmd.Principal.Amount)
    {
      return null;
    }

    lender.PostLoanDisbursement(borrower, cmd.Principal, hour.Date);
    var id = nextId();
    return new Loan(
      id,
      cmd.LenderFirmId,
      cmd.BorrowerFirmId,
      cmd.Principal,
      cmd.AnnualInterestRate,
      hour,
      hour.AddHours(cmd.TermHours));
  }

  /// <summary>
  /// Disburses a loan funded from household budget (caller already validated comfort + debit).
  /// </summary>
  public static Loan? TryOriginateHouseholdLender<TLedger>(
    IDictionary<FirmId, TLedger> ledgers,
    OriginateLoan cmd,
    SimulationHour hour,
    Func<LoanId> nextId)
    where TLedger : ILoanLedger<SimulationDate>
  {
    if (cmd.Principal.Amount <= 0m
        || cmd.TermHours <= 0
        || cmd.LenderFirmId.Equals(cmd.BorrowerFirmId)
        || !ledgers.TryGetValue(cmd.LenderFirmId, out var lender)
        || !ledgers.TryGetValue(cmd.BorrowerFirmId, out var borrower))
    {
      return null;
    }

    lender.PostHouseholdLoanDisbursement(borrower, cmd.Principal, hour.Date);
    var id = nextId();
    return new Loan(
      id,
      cmd.LenderFirmId,
      cmd.BorrowerFirmId,
      cmd.Principal,
      cmd.AnnualInterestRate,
      hour,
      hour.AddHours(cmd.TermHours));
  }

  /// <summary>Capitalizes one hour of interest onto principal / notes.</summary>
  public static Money AccrueHour<TLedger>(
    Loan loan,
    IDictionary<FirmId, TLedger> ledgers,
    SimulationHour hour)
    where TLedger : ILoanLedger<SimulationDate>
  {
    if (loan.Status != LoanStatus.Active)
    {
      return Money.Zero;
    }

    var interest = loan.HourlyInterest();
    if (interest.Amount <= 0m
        || !ledgers.TryGetValue(loan.LenderFirmId, out var lender)
        || !ledgers.TryGetValue(loan.BorrowerFirmId, out var borrower))
    {
      return Money.Zero;
    }

    lender.PostInterestAccrual(borrower, interest, hour.Date);
    loan.PrincipalRemaining = Money.From(loan.PrincipalRemaining.Amount + interest.Amount);
    loan.AccruedInterest = Money.From(loan.AccruedInterest.Amount + interest.Amount);
    return interest;
  }

  /// <summary>Applies cash repayment up to <paramref name="amount"/> (or borrower cash).</summary>
  public static Money TryRepay<TLedger>(
    Loan loan,
    IDictionary<FirmId, TLedger> ledgers,
    Money amount,
    SimulationHour hour,
    bool lenderIsHousehold = false,
    Action<FirmId, Money>? creditHouseholdBudget = null)
    where TLedger : ILoanLedger<SimulationDate>
  {
    if (loan.Status is not LoanStatus.Active and not LoanStatus.Defaulted
        || amount.Amount <= 0m
        || !ledgers.TryGetValue(loan.LenderFirmId, out var lender)
        || !ledgers.TryGetValue(loan.BorrowerFirmId, out var borrower))
    {
      return Money.Zero;
    }

    var pay = Math.Min(amount.Amount, Math.Min(borrower.Cash.Amount, loan.PrincipalRemaining.Amount));
    if (pay <= 0m)
    {
      return Money.Zero;
    }

    var money = Money.From(pay);
    if (lenderIsHousehold)
    {
      lender.PostHouseholdLoanRepayment(borrower, money, hour.Date);
      creditHouseholdBudget?.Invoke(loan.LenderFirmId, money);
    }
    else
    {
      lender.PostLoanRepayment(borrower, money, hour.Date);
    }

    loan.PrincipalRemaining = Money.From(loan.PrincipalRemaining.Amount - pay);
    if (loan.PrincipalRemaining.Amount <= 0.0000001m)
    {
      loan.PrincipalRemaining = Money.Zero;
      loan.Status = LoanStatus.Closed;
    }

    return money;
  }
}
