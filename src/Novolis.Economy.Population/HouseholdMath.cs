namespace Novolis.Economy.Population;

/// <summary>Household count and labor helpers from cohort headcount.</summary>
public static class HouseholdMath
{
  /// <summary>Households from headcount (at least 1 when population &gt; 0).</summary>
  public static int Count(PopulationCount population, int peoplePerHousehold = 4)
  {
    if (population.Value <= 0 || peoplePerHousehold <= 0)
    {
      return 0;
    }

    return Math.Max(1, (int)(population.Value / peoplePerHousehold));
  }

  /// <summary>Productive labor hours per calendar day for a cohort.</summary>
  public static decimal LaborHoursPerDay(
    PopulationCount population,
    HouseholdProductivityKind productivity,
    int peoplePerHousehold = 4) =>
    Count(population, peoplePerHousehold) * HouseholdProductivity.HoursPerDay(productivity);

  /// <summary>Labor hours available in one simulation hour.</summary>
  public static decimal LaborHoursPerTick(
    PopulationCount population,
    HouseholdProductivityKind productivity,
    int peoplePerHousehold = 4) =>
    LaborHoursPerDay(population, productivity, peoplePerHousehold) / 24m;

  /// <summary>Comfort floor: threshold per household times household count.</summary>
  public static Money ComfortFloor(
    PopulationCount population,
    Money thresholdPerHousehold,
    int peoplePerHousehold = 4) =>
    Money.From(thresholdPerHousehold.Amount * Count(population, peoplePerHousehold));
}
