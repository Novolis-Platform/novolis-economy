namespace Novolis.Economy;

/// <summary>Household productive hours per day (setting on cohort/household).</summary>
public enum HouseholdProductivityKind
{
  /// <summary>12 productive hours per household per day.</summary>
  Common = 0,

  /// <summary>18 productive hours per household per day (default).</summary>
  Mean = 1,

  /// <summary>24 productive hours per household per day.</summary>
  Extreme = 2,
}

/// <summary>Maps <see cref="HouseholdProductivityKind"/> to hours.</summary>
public static class HouseholdProductivity
{
  /// <summary>Productive hours per household per calendar day.</summary>
  public static decimal HoursPerDay(HouseholdProductivityKind kind) => kind switch
  {
    HouseholdProductivityKind.Common => 12m,
    HouseholdProductivityKind.Extreme => 24m,
    _ => 18m,
  };
}
