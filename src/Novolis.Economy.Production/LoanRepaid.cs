using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Cash repayment applied to a loan.</summary>
public sealed record LoanRepaid(
  SimulationHour Hour,
  LoanId LoanId,
  Money Amount,
  Money PrincipalRemaining) : IEconomyEvent;
