using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Finance;

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
