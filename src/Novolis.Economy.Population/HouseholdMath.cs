namespace Novolis.Economy.Population;

/// <summary>
/// Household count and labor helpers. Economic resolution stops at the household;
/// there is no headcount or people-per-household layer.
/// </summary>
public static class HouseholdMath
{
  /// <summary>
  /// Households in the cohort (<see cref="PopulationCount"/> is household count).
  /// </summary>
  public static int Count(PopulationCount households) =>
    households.Value <= 0 ? 0 : (int)households.Value;

  /// <summary>Productive labor hours per calendar day for a cohort.</summary>
  public static decimal LaborHoursPerDay(
    PopulationCount households,
    HouseholdProductivityKind productivity) =>
    Count(households) * HouseholdProductivity.HoursPerDay(productivity);

  /// <summary>Labor hours available in one simulation hour.</summary>
  public static decimal LaborHoursPerTick(
    PopulationCount households,
    HouseholdProductivityKind productivity) =>
    LaborHoursPerDay(households, productivity) / 24m;

  /// <summary>Comfort floor: threshold per household times household count.</summary>
  public static Money ComfortFloor(
    PopulationCount households,
    Money thresholdPerHousehold) =>
    Money.From(thresholdPerHousehold.Amount * Count(households));
}

