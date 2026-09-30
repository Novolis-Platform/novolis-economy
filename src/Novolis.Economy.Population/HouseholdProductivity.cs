namespace Novolis.Economy;

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

