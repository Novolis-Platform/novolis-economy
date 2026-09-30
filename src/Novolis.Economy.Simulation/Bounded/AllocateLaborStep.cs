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

/// <summary>3. Allocate regional labor to activities (pro-rata by labor demand).</summary>
public sealed class AllocateLaborStep : IBoundedPeriodStep
{
    public string Name => "03_AllocateLabor";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var allocation = new Dictionary<ActivityId, decimal>();
        foreach (var regionId in current.Regions.Keys)
        {
            var supply = current.Scratch.LaborSupplyByRegion.TryGetValue(regionId, out var s)
                ? s
                : LaborSupply.Calculate(current, regionId);
            var acts = current.Activities.Values.Where(a => a.RegionId.Equals(regionId)).ToList();
            var demand = acts.Sum(a => a.InstalledCapacity * a.Recipe.LaborHoursPerRun);
            if (demand <= 0m || supply <= 0m)
            {
                foreach (var a in acts)
                    allocation[a.Id] = 0m;
                continue;
            }

            var scale = Math.Min(1m, supply / demand);
            foreach (var a in acts)
                allocation[a.Id] = a.InstalledCapacity * a.Recipe.LaborHoursPerRun * scale;
        }

        return current with
        {
            Scratch = current.Scratch with { LaborAllocated = allocation }
        };
    }
}
