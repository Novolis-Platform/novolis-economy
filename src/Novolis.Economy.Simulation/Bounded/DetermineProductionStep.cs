using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Labor;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transport;
using Novolis.Economy.Core.Transactions;
using CoreLegalEntityKind = Novolis.Economy.Core.LegalEntityKind;

using Novolis.Economy.Simulation.Bounded;

namespace Novolis.Economy.Simulation.Bounded;

/// <summary>4. Determine activity production (min constraints).</summary>
public sealed class DetermineProductionStep : IBoundedPeriodStep
{
    public string Name => "04_DetermineProduction";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var runs = new Dictionary<ActivityId, decimal>();
        var committed = new Dictionary<RegionId, decimal>();

        foreach (var activity in current.Activities.Values.OrderBy(a => a.Id.Value))
        {
            var already = committed.GetValueOrDefault(activity.RegionId);
            // Prefer allocated labor ceiling when present
            var allocated = current.Scratch.LaborAllocated.TryGetValue(activity.Id, out var lab)
                ? lab
                : decimal.MaxValue;

            var byLaborAlloc = activity.Recipe.LaborHoursPerRun <= 0m
                ? activity.InstalledCapacity
                : allocated / activity.Recipe.LaborHoursPerRun;

            var calculated = ProductionCalculator.ActualRuns(current, activity, already);
            var actual = Math.Floor(Math.Min(calculated, byLaborAlloc));
            runs[activity.Id] = actual;
            committed[activity.RegionId] = already + actual * activity.Recipe.LaborHoursPerRun;
        }

        return current with
        {
            Scratch = current.Scratch with { ActualRuns = runs }
        };
    }
}
