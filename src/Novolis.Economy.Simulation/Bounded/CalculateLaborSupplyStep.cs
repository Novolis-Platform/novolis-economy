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

/// <summary>2. Calculate household labor supply.</summary>
public sealed class CalculateLaborSupplyStep : IBoundedPeriodStep
{
    public string Name => "02_CalculateLaborSupply";

    public BoundedPeriodState Execute(BoundedPeriodState current)
    {
        var byRegion = new Dictionary<RegionId, decimal>();
        foreach (var regionId in current.Regions.Keys)
            byRegion[regionId] = LaborSupply.Calculate(current, regionId);
        return current with
        {
            Scratch = current.Scratch with { LaborSupplyByRegion = byRegion }
        };
    }
}
