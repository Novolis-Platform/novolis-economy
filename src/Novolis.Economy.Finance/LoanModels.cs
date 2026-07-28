using Novolis.Economy;

namespace Novolis.Economy.Finance;

/// <summary>Lifecycle of an inter-firm term loan.</summary>
public enum LoanStatus
{
  /// <summary>Accruing; not yet fully repaid.</summary>
  Active = 0,
  /// <summary>Missed required repayment at term.</summary>
  Defaulted = 1,
  /// <summary>Principal and accrued interest cleared.</summary>
  Closed = 2,
}

/// <summary>Inter-firm term loan (working capital).</summary>
public sealed class Loan
{
  /// <summary>Creates a loan contract.</summary>
  public Loan(
    LoanId id,
    FirmId lenderFirmId,
    FirmId borrowerFirmId,
    Money principal,
    decimal annualInterestRate,
    SimulationHour originatedAt,
    SimulationHour dueAt)
  {
    Id = id;
    LenderFirmId = lenderFirmId;
    BorrowerFirmId = borrowerFirmId;
    PrincipalRemaining = principal;
    AnnualInterestRate = annualInterestRate;
    AccruedInterest = Money.Zero;
    OriginatedAt = originatedAt;
    DueAt = dueAt;
    Status = LoanStatus.Active;
  }

  /// <summary>Loan id.</summary>
  public LoanId Id { get; }

  /// <summary>Lender firm.</summary>
  public FirmId LenderFirmId { get; }

  /// <summary>Borrower firm.</summary>
  public FirmId BorrowerFirmId { get; }

  /// <summary>Outstanding principal (includes capitalized interest when accrued onto notes).</summary>
  public Money PrincipalRemaining { get; set; }

  /// <summary>Annualized interest rate (e.g. 0.12 = 12%/year).</summary>
  public decimal AnnualInterestRate { get; }

  /// <summary>Interest accrued this period pending capitalization (diagnostic).</summary>
  public Money AccruedInterest { get; set; }

  /// <summary>Disbursement hour.</summary>
  public SimulationHour OriginatedAt { get; }

  /// <summary>Hour when full repayment is due.</summary>
  public SimulationHour DueAt { get; }

  /// <summary>Lifecycle status.</summary>
  public LoanStatus Status { get; set; }

  /// <summary>Hours in a year for hourly accrual (24 × 365).</summary>
  public const decimal HoursPerYear = 24m * 365m;

  /// <summary>Interest for one simulation hour on current principal.</summary>
  public Money HourlyInterest()
  {
    if (PrincipalRemaining.Amount <= 0m || AnnualInterestRate <= 0m)
    {
      return Money.Zero;
    }

    var amount = PrincipalRemaining.Amount * AnnualInterestRate / HoursPerYear;
    return Money.From(Math.Round(amount, 6, MidpointRounding.AwayFromZero));
  }
}

/// <summary>Pure loan settlement helpers (ledger + loan mutation).</summary>
public static class LoanEngine
{
  /// <summary>
  /// Disburses a new loan when the lender has cash. Returns null on failure.
  /// </summary>
  public static Loan? TryOriginate(
    IDictionary<FirmId, Novolis.Economy.Accounting.FirmLedger> ledgers,
    OriginateLoan cmd,
    SimulationHour hour,
    Func<LoanId> nextId)
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

    Novolis.Economy.Accounting.LedgerEngine.PostLoanDisbursement(
      lender, borrower, cmd.Principal, hour.Date);
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
  public static Loan? TryOriginateHouseholdLender(
    IDictionary<FirmId, Novolis.Economy.Accounting.FirmLedger> ledgers,
    OriginateLoan cmd,
    SimulationHour hour,
    Func<LoanId> nextId)
  {
    if (cmd.Principal.Amount <= 0m
        || cmd.TermHours <= 0
        || cmd.LenderFirmId.Equals(cmd.BorrowerFirmId)
        || !ledgers.TryGetValue(cmd.LenderFirmId, out var lender)
        || !ledgers.TryGetValue(cmd.BorrowerFirmId, out var borrower))
    {
      return null;
    }

    Novolis.Economy.Accounting.LedgerEngine.PostHouseholdLoanDisbursement(
      lender, borrower, cmd.Principal, hour.Date);
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
  public static Money AccrueHour(
    Loan loan,
    IDictionary<FirmId, Novolis.Economy.Accounting.FirmLedger> ledgers,
    SimulationHour hour)
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

    Novolis.Economy.Accounting.LedgerEngine.PostInterestAccrual(lender, borrower, interest, hour.Date);
    loan.PrincipalRemaining = Money.From(loan.PrincipalRemaining.Amount + interest.Amount);
    loan.AccruedInterest = Money.From(loan.AccruedInterest.Amount + interest.Amount);
    return interest;
  }

  /// <summary>Applies cash repayment up to <paramref name="amount"/> (or borrower cash).</summary>
  public static Money TryRepay(
    Loan loan,
    IDictionary<FirmId, Novolis.Economy.Accounting.FirmLedger> ledgers,
    Money amount,
    SimulationHour hour,
    bool lenderIsHousehold = false,
    Action<FirmId, Money>? creditHouseholdBudget = null)
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
      Novolis.Economy.Accounting.LedgerEngine.PostHouseholdLoanRepayment(
        lender, borrower, money, hour.Date);
      creditHouseholdBudget?.Invoke(loan.LenderFirmId, money);
    }
    else
    {
      Novolis.Economy.Accounting.LedgerEngine.PostLoanRepayment(lender, borrower, money, hour.Date);
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
