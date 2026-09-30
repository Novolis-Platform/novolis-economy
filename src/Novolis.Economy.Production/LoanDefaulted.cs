using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Borrower missed a required repayment.</summary>
public sealed record LoanDefaulted(
  SimulationHour Hour,
  LoanId LoanId,
  FirmId BorrowerFirmId,
  Money PrincipalRemaining) : IEconomyEvent;
