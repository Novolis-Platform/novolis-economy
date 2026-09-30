namespace Novolis.Economy.Core;

/// <summary>Behavioral profile for a cohort (SPEC §4).</summary>
public sealed record HouseholdProfile(
  decimal ConsumptionWeight,
  decimal SavingsPreference,
  decimal LaborQuality,
  decimal MigrationPreference);