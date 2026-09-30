namespace Novolis.Economy.Core;

/// <summary>Labor-hours per household-day — capacity, not productivity (SPEC §5).</summary>
public enum HouseholdLaborKind
{
  Common = 0,
  Mean,
  Extreme
}