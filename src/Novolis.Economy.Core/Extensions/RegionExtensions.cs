namespace Novolis.Economy.Core.Extensions;

/// <summary>Insight helpers on regions (require economy context for utilization).</summary>
public static class RegionExtensions
{
    /// <summary>Capacity / labor insight for this region in <paramref name="state"/>.</summary>
    public static RegionInsight ToInsight(this Region region, EconomyState state) =>
        state.InsightFor(region.Id);

    /// <summary>Living utilization in [0, ∞); &gt;1 means over capacity.</summary>
    public static decimal LivingUtilization(this Region region, EconomyState state) =>
        state.InsightFor(region.Id).LivingUtilization;

    /// <summary>Production-space utilization.</summary>
    public static decimal ProductionUtilization(this Region region, EconomyState state) =>
        state.InsightFor(region.Id).ProductionUtilization;
}
