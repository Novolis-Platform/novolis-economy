using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Loan funds disbursed.</summary>
public sealed record LoanOriginated(
  SimulationHour Hour,
  LoanId LoanId,
  FirmId LenderFirmId,
  FirmId BorrowerFirmId,
  Money Principal,
  decimal AnnualInterestRate,
  SimulationHour DueAt) : IEconomyEvent;
