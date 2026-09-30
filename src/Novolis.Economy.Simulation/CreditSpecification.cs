using Novolis.Economy.Markets;

namespace Novolis.Economy.Simulation;

/// <summary>Parameters for automatic credit draws and default timing.</summary>
public sealed record CreditSpecification(
  decimal FacilityInterestRatePerPeriod = 0.01m,
  int FacilityTermPeriods = 4,
  int DelinquencyPeriodsBeforeDefault = 2);
