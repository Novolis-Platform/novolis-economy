using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class AllocateLaborPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.AllocateLabor;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    world.AllocatedLaborHours.Clear();

    var crewDemand = new Dictionary<FirmId, decimal>();
    foreach (var shipment in world.Shipments.Where(s => !s.IsLegacy && s.Phase == ShipmentPhase.Underway && s.Vehicle is not null))
    {
      var hours = shipment.Vehicle!.CrewLaborPerUnderwayHour;
      crewDemand[shipment.FirmId] = crewDemand.GetValueOrDefault(shipment.FirmId) + hours;
    }

    if (world.RegionLaborPoolsActive)
    {
      ApplyRegionLaborPools(world);
    }

    foreach (var (firmId, available) in world.AvailableLaborHours.OrderBy(kv => kv.Key.Value))
    {
      if (world.IsHousehold(firmId))
      {
        continue;
      }

      var planned = world.ProductionPlans
        .Where(p => p.Key.Firm == firmId)
        .Sum(p => p.Value.Value * world.Policy.LaborHoursPerOutputUnit);
      var crew = crewDemand.GetValueOrDefault(firmId);
      var crewAllocated = Math.Min(crew, available);
      var manufacturingCap = Math.Max(0m, available - crewAllocated);
      var allocated = Math.Min(manufacturingCap, planned);
      world.AllocatedLaborHours[firmId] = allocated;
      var wage = Money.From((allocated + crewAllocated) * world.Policy.WageRatePerHour.Amount);
      if (wage.Amount <= 0m || !world.Ledgers.TryGetValue(firmId, out var ledger))
      {
        continue;
      }

      LedgerEngine.AccrueWages(ledger, wage, context.State.Clock.Date);
      world.AccruedWages[firmId] = world.AccruedWages.GetValueOrDefault(firmId) + wage;
    }

    return ValueTask.CompletedTask;
  }

  /// <summary>
  /// Sets firm <see cref="EconomyWorld.AvailableLaborHours"/> from per-area household pools.
  /// </summary>
  public static void ApplyRegionLaborPools(EconomyWorld world)
  {
    var pools = new Dictionary<GeographicAreaId, decimal>();
    foreach (var cohort in world.Cohorts)
    {
      var area = cohort.Definition.Area;
      if (!world.Regions.ContainsKey(area))
      {
        continue;
      }

      var hours = HouseholdMath.LaborHoursPerTick(
        cohort.Definition.Population,
        cohort.Definition.Productivity);
      pools[area] = pools.GetValueOrDefault(area) + hours;
    }

    var remaining = pools.ToDictionary(kv => kv.Key, kv => kv.Value);
    var firmAvailable = new Dictionary<FirmId, decimal>();

    // Manufacturing demand by area → firm (deterministic firm order).
    var demandByArea = new Dictionary<GeographicAreaId, List<(FirmId Firm, decimal Hours)>>();
    foreach (var plan in world.ProductionPlans.OrderBy(p => p.Key.Firm.Value).ThenBy(p => p.Key.Facility.Value))
    {
      if (!world.Facilities.TryGetValue(plan.Key.Facility, out var facility)
          || facility.Area is not { } area
          || !world.Regions.ContainsKey(area))
      {
        continue;
      }

      var hours = plan.Value.Value * world.Policy.LaborHoursPerOutputUnit;
      if (hours <= 0m)
      {
        continue;
      }

      if (!demandByArea.TryGetValue(area, out var list))
      {
        list = [];
        demandByArea[area] = list;
      }

      var idx = list.FindIndex(x => x.Firm.Equals(plan.Key.Firm));
      if (idx < 0)
      {
        list.Add((plan.Key.Firm, hours));
      }
      else
      {
        list[idx] = (plan.Key.Firm, list[idx].Hours + hours);
      }
    }

    foreach (var area in demandByArea.Keys.OrderBy(a => a.Value))
    {
      var poolLeft = remaining.GetValueOrDefault(area);
      foreach (var (firm, hours) in demandByArea[area].OrderBy(x => x.Firm.Value))
      {
        var take = Math.Min(hours, poolLeft);
        if (take <= 0m)
        {
          continue;
        }

        firmAvailable[firm] = firmAvailable.GetValueOrDefault(firm) + take;
        poolLeft -= take;
      }

      remaining[area] = poolLeft;
    }

    // Firms with region-only mfg demand get pool supply (legacy SetLabor ignored for those).
    // Crew-only firms (carriers with storage posts) keep SetLabor.
    foreach (var firmId in world.Firms.Keys.OrderBy(f => f.Value))
    {
      if (world.IsHousehold(firmId))
      {
        world.AvailableLaborHours[firmId] = 0m;
        continue;
      }

      if (firmAvailable.TryGetValue(firmId, out var fromPool))
      {
        world.AvailableLaborHours[firmId] = fromPool;
        continue;
      }

      var facilities = world.Facilities.Values.Where(f => f.FirmId.Equals(firmId)).ToList();
      var hasRegionMfg = facilities.Any(f =>
        f.Area is { } a
        && world.Regions.ContainsKey(a)
        && EconomicRegion.ConsumesProductionSlot(f.Layout));
      if (hasRegionMfg)
      {
        world.AvailableLaborHours[firmId] = 0m;
      }
      // else keep builder / SetAvailableLabor value for crew-only operators
    }
  }
}
