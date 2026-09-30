namespace Novolis.Economy;

/// <summary>
/// Household productive hours per day (setting on cohort/household).
/// Resolution stops at the household; there is no headcount layer.
/// </summary>
public enum HouseholdProductivityKind
{
  /// <summary>12 hours per household per day. Scarcer labor, calmer polity.</summary>
  Common = 0,

  /// <summary>18 hours per household per day. Default.</summary>
  Mean = 1,

  /// <summary>
  /// 24 hours per household per day. Full household engine
  /// (adults and dependents sold into the pool).
  /// </summary>
  Extreme = 2,
}

