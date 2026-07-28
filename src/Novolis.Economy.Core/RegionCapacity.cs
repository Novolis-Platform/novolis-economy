namespace Novolis.Economy.Core;

/// <summary>Region capacity helpers (SPEC §3).</summary>
public static class RegionCapacity
{
    /// <summary>Households currently residing in the region.</summary>
    public static int OccupiedLiving(EconomyState state, RegionId regionId) =>
        state.Cohorts.Values.Where(c => c.RegionId.Equals(regionId)).Sum(c => c.HouseholdCount);

    /// <summary>Remaining living slots (may be negative if over capacity — invariant failure).</summary>
    public static int RemainingLiving(EconomyState state, Region region) =>
        region.LivingCapacity - OccupiedLiving(state, region.Id);

    /// <summary>Production space already reserved by installed activities.</summary>
    public static decimal InstalledProductionSpace(EconomyState state, RegionId regionId) =>
        state.Activities.Values
            .Where(a => a.RegionId.Equals(regionId))
            .Sum(a => a.InstalledCapacity * a.Recipe.ProductionSpacePerRun);

    /// <summary>Remaining production capacity.</summary>
    public static decimal RemainingProduction(EconomyState state, Region region) =>
        region.ProductionCapacity - InstalledProductionSpace(state, region.Id);

    /// <summary>Logistics load from in-flight transfers originating here this period.</summary>
    public static decimal LogisticsLoad(EconomyState state, RegionId regionId) =>
        state.Transfers
            .Where(t => t.Origin.Equals(regionId))
            .Sum(t => t.Quantity);

    /// <summary>Remaining logistics capacity.</summary>
    public static decimal RemainingLogistics(EconomyState state, Region region) =>
        region.LogisticsCapacity - LogisticsLoad(state, region.Id);

    /// <summary>Clamp desired install runs by remaining production space.</summary>
    public static decimal MaxInstallableRuns(EconomyState state, Region region, ActivityRecipe recipe)
    {
        if (recipe.ProductionSpacePerRun <= 0m)
            return decimal.MaxValue;
        var remaining = RemainingProduction(state, region);
        return remaining <= 0m ? 0m : remaining / recipe.ProductionSpacePerRun;
    }
}
