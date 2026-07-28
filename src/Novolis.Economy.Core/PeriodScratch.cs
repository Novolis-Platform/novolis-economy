namespace Novolis.Economy.Core;

/// <summary>Ephemeral per-period scratch used by the default pipeline (cleared each period).</summary>
public sealed record PeriodScratch(
    IReadOnlyDictionary<RegionId, decimal> LaborSupplyByRegion,
    IReadOnlyDictionary<ActivityId, decimal> LaborAllocated,
    IReadOnlyDictionary<ActivityId, decimal> ActualRuns)
{
    /// <summary>Empty scratch.</summary>
    public static PeriodScratch Empty { get; } = new(
        new Dictionary<RegionId, decimal>(),
        new Dictionary<ActivityId, decimal>(),
        new Dictionary<ActivityId, decimal>());
}
