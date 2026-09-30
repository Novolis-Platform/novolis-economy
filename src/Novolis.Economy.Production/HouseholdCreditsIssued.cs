using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Wage cash redistributed to household cohort budgets.</summary>
public sealed record HouseholdCreditsIssued(
  SimulationHour Hour,
  FirmId FirmId,
  Money Amount) : IEconomyEvent;
