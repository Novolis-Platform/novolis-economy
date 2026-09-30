using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Interest added to loan balance.</summary>
public sealed record InterestAccrued(
  SimulationHour Hour,
  LoanId LoanId,
  Money Amount) : IEconomyEvent;
