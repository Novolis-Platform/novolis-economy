namespace Novolis.Economy.Core;

/// <summary>Maps labor kind to hours per household-day.</summary>
public static class HouseholdLabor
{
    /// <summary>Labor-hours one average household supplies per day.</summary>
    public static decimal HoursPerDay(HouseholdLaborKind kind) =>
        kind switch
        {
            HouseholdLaborKind.Common => 12m,
            HouseholdLaborKind.Mean => 18m,
            HouseholdLaborKind.Extreme => 24m,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}