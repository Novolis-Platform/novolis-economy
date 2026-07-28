namespace Novolis.Economy.Core.Labor;

/// <summary>Regional labor supply = Σ count × hours × quality (SPEC §5).</summary>
public static class LaborSupply
{
    /// <summary>Total effective labor-hours in a region from all cohorts.</summary>
    public static decimal Calculate(EconomyState state, RegionId regionId) =>
        state.Cohorts.Values
            .Where(c => c.RegionId.Equals(regionId))
            .Sum(HouseholdMath.EffectiveLaborHours);

    /// <summary>Remaining labor after subtracting already-committed hours this period.</summary>
    public static decimal Remaining(EconomyState state, RegionId regionId, decimal committedHours) =>
        Math.Max(0m, Calculate(state, regionId) - committedHours);
}
