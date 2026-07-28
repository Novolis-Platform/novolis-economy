using System.Collections.Immutable;
using System.Diagnostics;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Logistics;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using TUnit.Core;

namespace Novolis.Economy.Unit.Scenarios;

/// <summary>
/// Space-skinned tramp freighter scenarios on the same hub/corridor/fuel model
/// (no Astro coupling). An independent operator hauls speculative cargo between
/// starport hubs, bunkers mid-route, pays jump-lane tolls, and accrues crew wages.
/// </summary>
public sealed class TrampFreighterScenarioTests
{
  [Test]
  public async Task TrampCircuit_HaulsOreToCore_SellsAndCoversOpex()
  {
    var (sim, ids) = CreateTrampWorld();
    var openingCash = sim.State.World.Ledgers[ids.Tramp].Cash.Amount;

    // Speculative buy at the frontier outpost, then one hull at a time to Core Port.
    sim.Enqueue(new PlaceProcurementOrder(
      ids.Tramp, ids.LocFrontier, ids.Ore, Quantity.From(25m), Money.From(2m)));
    sim.Enqueue(new PlaceProcurementOrder(
      ids.Tramp, ids.LocFrontier, ids.Fuel, Quantity.From(40m), Money.From(1m)));
    sim.Enqueue(new PlaceProcurementOrder(
      ids.Tramp, ids.LocWaystation, ids.Fuel, Quantity.From(40m), Money.From(1.5m)));
    await AdvanceUntilIdle(sim, maxHours: 2);

    sim.Enqueue(new PlanShipment(
      ids.Tramp,
      ids.HubFrontier.Value,
      ids.HubCore.Value,
      ids.Ore,
      Quantity.From(25m),
      ids.TrampHull.Value));

    await AdvanceUntilDelivered(sim, ids.Tramp, ids.Ore, ids.LocCore, Quantity.From(25m), maxHours: 120);

    // Post a retail ask at Core Port; local demand lifts cargo into cash.
    sim.Enqueue(new SetRetailPrice(ids.Tramp, ids.CoreFacility, ids.Ore, Money.From(8m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(48));

    var world = sim.State.World;
    var ledger = world.Ledgers[ids.Tramp];
    var sold = sim.State.Events.OfType<GoodsSold>().Where(e => e.FirmId.Equals(ids.Tramp)).Sum(e => e.Quantity.Value);
    var revenue = sim.State.Events.OfType<GoodsSold>().Where(e => e.FirmId.Equals(ids.Tramp)).Sum(e => e.Revenue.Amount);

    await Assert.That(sold).IsGreaterThan(0m);
    await Assert.That(world.TransportStats.FuelBurned.Value).IsGreaterThan(0m);
    await Assert.That(world.TransportStats.TollsPaid.Amount).IsGreaterThan(0m);
    await Assert.That(world.TransportStats.CrewLaborHours).IsGreaterThan(0m);
    await Assert.That(world.TransportStats.TransitSampleCount).IsEqualTo(1);
    await Assert.That(sim.State.Events.OfType<ShipmentHubArrived>().Any()).IsTrue();

    // Opex hit cash before sales; after sales the tramp should not be wiped out.
    await Assert.That(ledger.Cash.Amount).IsGreaterThan(openingCash * 0.2m);
    await Assert.That(revenue).IsGreaterThan(0m);

    var sum = Enum.GetValues<AccountRole>().Sum(r => ledger.Balance(r).Amount);
    await Assert.That(Math.Abs(sum)).IsLessThan(0.01m);
  }

  [Test]
  public async Task TrampCircuit_SequentialJobs_ReturnLegAndWorkingCapitalLockup()
  {
    var (sim, ids) = CreateTrampWorld();

    // Job 1: ore frontier → core
    SeedTrampCargoAndFuel(sim, ids, oreAtFrontier: 20m, fuelFrontier: 30m, fuelWay: 30m, fuelCore: 20m);
    sim.Enqueue(new PlanShipment(
      ids.Tramp, ids.HubFrontier.Value, ids.HubCore.Value, ids.Ore, Quantity.From(20m), ids.TrampHull.Value));

    var departedAt = sim.State.Clock;
    await AdvanceUntilDelivered(sim, ids.Tramp, ids.Ore, ids.LocCore, Quantity.From(20m), maxHours: 120);
    var firstTransit = sim.State.Clock.HourIndex - departedAt.HourIndex;
    await Assert.That(firstTransit).IsGreaterThan(10);

    // While the first haul was underway, destination shelves stayed empty (working capital lockup).
    // After delivery, cargo sits at Core — buy return cargo and haul home.
    sim.Enqueue(new PlaceProcurementOrder(
      ids.Tramp, ids.LocCore, ids.Parts, Quantity.From(15m), Money.From(3m)));
    await AdvanceUntilIdle(sim, maxHours: 2);

    // Only one tramp hull: do not overlap PlanShipment while prior cargo still needs the ship.
    await Assert.That(sim.State.World.Shipments.Count(s => !s.IsLegacy && s.Status == ShipmentStatus.InTransit))
      .IsEqualTo(0);

    sim.Enqueue(new PlanShipment(
      ids.Tramp, ids.HubCore.Value, ids.HubFrontier.Value, ids.Parts, Quantity.From(15m), ids.TrampHull.Value));
    await AdvanceUntilDelivered(sim, ids.Tramp, ids.Parts, ids.LocFrontier, Quantity.From(15m), maxHours: 120);

    var stats = sim.State.World.TransportStats;
    await Assert.That(stats.CargoDelivered.Value).IsEqualTo(35m);
    await Assert.That(stats.TransitSampleCount).IsEqualTo(2);
    await Assert.That(stats.FailedPlans).IsEqualTo(0);
  }

  [Test]
  public async Task Tramp_RejectsSparseRimDirect_WhenTankCannotMakeTheJump()
  {
    var (sim, ids) = CreateTrampWorld();
    SeedTrampCargoAndFuel(sim, ids, oreAtFrontier: 10m, fuelFrontier: 50m, fuelWay: 30m, fuelCore: 0m);

    // Direct frontier → sparse rim burns more than tank capacity and has no staging path.
    sim.Enqueue(new PlanShipment(
      ids.Tramp, ids.HubFrontier.Value, ids.HubRim.Value, ids.Ore, Quantity.From(10m), ids.TrampHull.Value));
    await sim.AdvanceAsync(SimulationDuration.FromHours(3));

    await Assert.That(sim.State.World.TransportStats.FailedPlans).IsGreaterThan(0);
    await Assert.That(sim.State.Events.OfType<ShipmentPlanFailed>().Any(e => e.Reason.Contains("path"))).IsTrue();

    // Same tramp can still work the main lane (frontier → waystation → core).
    sim.Enqueue(new PlanShipment(
      ids.Tramp, ids.HubFrontier.Value, ids.HubCore.Value, ids.Ore, Quantity.From(10m), ids.TrampHull.Value));
    await AdvanceUntilDelivered(sim, ids.Tramp, ids.Ore, ids.LocCore, Quantity.From(10m), maxHours: 120);
  }

  [Test]
  public async Task Tramp_StuckWhenWaystationFuelStockout_ThenRecoversAfterBunkerProcure()
  {
    var (sim, ids) = CreateTrampWorld(waystationFuelStock: 0m);
    // Enough fuel at frontier for the first leg only (4 burn); second leg needs waystation bunker.
    SeedTrampCargoAndFuel(sim, ids, oreAtFrontier: 10m, fuelFrontier: 4m, fuelWay: 0m, fuelCore: 0m);

    sim.Enqueue(new PlanShipment(
      ids.Tramp, ids.HubFrontier.Value, ids.HubCore.Value, ids.Ore, Quantity.From(10m), ids.TrampHull.Value));

    // Transit first leg + dwell; should arrive at waystation then stall without fuel for Core.
    await sim.AdvanceAsync(SimulationDuration.FromHours(30));
    var mid = sim.State.World.Inventory.GetQuantity(new InventoryKey(ids.Tramp, ids.LocCore, ids.Ore));
    await Assert.That(mid.Value).IsEqualTo(0m);
    await Assert.That(sim.State.World.Shipments.Any(s => !s.IsLegacy && s.Status == ShipmentStatus.InTransit)).IsTrue();

    // Procure bunker at the waystation; tramp resumes and completes.
    sim.Enqueue(new PlaceProcurementOrder(
      ids.Tramp, ids.LocWaystation, ids.Fuel, Quantity.From(20m), Money.From(1.5m)));
    await AdvanceUntilDelivered(sim, ids.Tramp, ids.Ore, ids.LocCore, Quantity.From(10m), maxHours: 80);
  }

  [Test]
  public async Task CompetingTraffic_BerthContentionAtBusyWaystation()
  {
    var (sim, ids) = CreateTrampWorld(waystationBerths: 1);
    // Liner and tramp both leave frontier toward core in the same window; waystation berth = 1.
    SeedTrampCargoAndFuel(sim, ids, oreAtFrontier: 10m, fuelFrontier: 40m, fuelWay: 40m, fuelCore: 0m);
    sim.State.World.Inventory.Add(
      new InventoryKey(ids.Liner, ids.LocFrontier, ids.Ore),
      new ProductBatch(ids.Ore, Quantity.From(10m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
    sim.State.World.Inventory.Add(
      new InventoryKey(ids.Liner, ids.LocFrontier, ids.Fuel),
      new ProductBatch(ids.Fuel, Quantity.From(40m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
    sim.State.World.Inventory.Add(
      new InventoryKey(ids.Liner, ids.LocWaystation, ids.Fuel),
      new ProductBatch(ids.Fuel, Quantity.From(40m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));

    sim.Enqueue(new PlanShipment(
      ids.Tramp, ids.HubFrontier.Value, ids.HubCore.Value, ids.Ore, Quantity.From(10m), ids.TrampHull.Value));
    sim.Enqueue(new PlanShipment(
      ids.Liner, ids.HubFrontier.Value, ids.HubCore.Value, ids.Ore, Quantity.From(10m), ids.LinerHull.Value));

    await AdvanceUntilDelivered(sim, ids.Tramp, ids.Ore, ids.LocCore, Quantity.From(10m), maxHours: 200);
    await AdvanceUntilDelivered(sim, ids.Liner, ids.Ore, ids.LocCore, Quantity.From(10m), maxHours: 200);

    // Both eventually deliver; contention only delays — economic scarcity of berths, not a hard fail.
    await Assert.That(sim.State.World.TransportStats.CargoDelivered.Value).IsEqualTo(20m);
    await Assert.That(sim.State.World.TransportStats.TransitSampleCount).IsEqualTo(2);
  }

  [Test]
  public async Task TrampCircuit_IsDeterministic()
  {
    async Task<ulong> RunAsync()
    {
      var (sim, ids) = CreateTrampWorld();
      SeedTrampCargoAndFuel(sim, ids, oreAtFrontier: 20m, fuelFrontier: 30m, fuelWay: 30m, fuelCore: 10m);
      sim.Enqueue(new PlanShipment(
        ids.Tramp, ids.HubFrontier.Value, ids.HubCore.Value, ids.Ore, Quantity.From(20m), ids.TrampHull.Value));
      await AdvanceUntilDelivered(sim, ids.Tramp, ids.Ore, ids.LocCore, Quantity.From(20m), maxHours: 120);
      sim.Enqueue(new SetRetailPrice(ids.Tramp, ids.CoreFacility, ids.Ore, Money.From(7m)));
      var result = await sim.AdvanceAsync(SimulationDuration.FromHours(24));
      return result.FinalHash;
    }

    var a = await RunAsync();
    var b = await RunAsync();
    await Assert.That(a).IsEqualTo(b);
  }

  [Test]
  public async Task TrampCircuit_MachineSpeedSmoke_PrintsSpaceLaneAggregates()
  {
    var sw = Stopwatch.StartNew();
    var (sim, ids) = CreateTrampWorld();
    SeedTrampCargoAndFuel(sim, ids, oreAtFrontier: 25m, fuelFrontier: 40m, fuelWay: 40m, fuelCore: 20m);

    sim.Enqueue(new PlanShipment(
      ids.Tramp, ids.HubFrontier.Value, ids.HubCore.Value, ids.Ore, Quantity.From(25m), ids.TrampHull.Value));
    await AdvanceUntilDelivered(sim, ids.Tramp, ids.Ore, ids.LocCore, Quantity.From(25m), maxHours: 120);

    sim.Enqueue(new SetRetailPrice(ids.Tramp, ids.CoreFacility, ids.Ore, Money.From(9m)));
    sim.Enqueue(new PlaceProcurementOrder(
      ids.Tramp, ids.LocCore, ids.Parts, Quantity.From(12m), Money.From(3m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(36));

    sim.Enqueue(new PlanShipment(
      ids.Tramp, ids.HubCore.Value, ids.HubFrontier.Value, ids.Parts, Quantity.From(12m), ids.TrampHull.Value));
    await AdvanceUntilDelivered(sim, ids.Tramp, ids.Parts, ids.LocFrontier, Quantity.From(12m), maxHours: 120);

    // Over-range attempt (economic connectivity scarcity)
    sim.Enqueue(new PlanShipment(
      ids.Tramp, ids.HubFrontier.Value, ids.HubRim.Value, ids.Parts, Quantity.From(5m), ids.TrampHull.Value));
    await sim.AdvanceAsync(SimulationDuration.FromHours(5));

    sw.Stop();
    var stats = sim.State.World.TransportStats;
    var ledger = sim.State.World.Ledgers[ids.Tramp];
    var wages = ledger.Balance(AccountRole.WageExpense).Amount;
    var fuelExp = ledger.Balance(AccountRole.TransportFuelExpense).Amount;
    var tollExp = ledger.Balance(AccountRole.TransportTollExpense).Amount;
    var avgTransit = stats.TransitSampleCount == 0
      ? 0.0
      : (double)stats.TransitHoursSum / stats.TransitSampleCount;

    Console.WriteLine(
      $"tramp freighter smoke: {sw.ElapsedMilliseconds}ms " +
      $"cargoDelivered={stats.CargoDelivered.Value} fuelBurned={stats.FuelBurned.Value} " +
      $"tolls={stats.TollsPaid.Amount} crewHours={stats.CrewLaborHours} " +
      $"wageExp={wages} fuelExp={fuelExp} tollExp={tollExp} " +
      $"avgTransit={avgTransit:F1}h failedPlans={stats.FailedPlans} " +
      $"cash={ledger.Cash.Amount} hash={sim.State.Hash:X16}");

    await Assert.That(stats.CargoDelivered.Value).IsEqualTo(37m);
    await Assert.That(stats.FailedPlans).IsGreaterThan(0);
    await Assert.That(sw.ElapsedMilliseconds).IsLessThan(5_000);
  }

  private static void SeedTrampCargoAndFuel(
    EconomySimulation sim,
    TrampIds ids,
    decimal oreAtFrontier,
    decimal fuelFrontier,
    decimal fuelWay,
    decimal fuelCore)
  {
    var inv = sim.State.World.Inventory;
    if (oreAtFrontier > 0m)
    {
      inv.Add(
        new InventoryKey(ids.Tramp, ids.LocFrontier, ids.Ore),
        new ProductBatch(ids.Ore, Quantity.From(oreAtFrontier), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
    }

    void AddFuel(InventoryLocationId loc, decimal qty)
    {
      if (qty <= 0m)
      {
        return;
      }

      inv.Add(
        new InventoryKey(ids.Tramp, loc, ids.Fuel),
        new ProductBatch(ids.Fuel, Quantity.From(qty), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
    }

    AddFuel(ids.LocFrontier, fuelFrontier);
    AddFuel(ids.LocWaystation, fuelWay);
    AddFuel(ids.LocCore, fuelCore);
  }

  private static async Task AdvanceUntilIdle(EconomySimulation sim, int maxHours)
  {
    for (var i = 0; i < maxHours; i++)
    {
      await sim.AdvanceAsync(SimulationDuration.FromHours(1));
      if (sim.State.World.PendingProcurement.Count == 0 &&
          sim.State.World.PendingPlanShipments.Count == 0 &&
          sim.State.World.PendingShipments.Count == 0)
      {
        return;
      }
    }
  }

  private static async Task AdvanceUntilDelivered(
    EconomySimulation sim,
    FirmId firm,
    ProductId product,
    InventoryLocationId destination,
    Quantity expected,
    int maxHours)
  {
    for (var i = 0; i < maxHours; i++)
    {
      var have = sim.State.World.Inventory.GetQuantity(new InventoryKey(firm, destination, product));
      if (have.Value + 0.0000001m >= expected.Value)
      {
        return;
      }

      await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    }

    var final = sim.State.World.Inventory.GetQuantity(new InventoryKey(firm, destination, product));
    throw new InvalidOperationException(
      $"Timed out waiting for delivery of {expected.Value} {product} to {destination}; have {final.Value}");
  }

  /// <summary>
  /// Starport network (space skin): Frontier Outpost ↔ Jump Waystation ↔ Core Port,
  /// plus an unreachable-without-staging Sparse Rim spoke that exceeds tramp tank range.
  /// </summary>
  private static (EconomySimulation Sim, TrampIds Ids) CreateTrampWorld(
    decimal waystationFuelStock = 0m,
    int waystationBerths = 2)
  {
    _ = waystationFuelStock;
    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      WageRatePerHour = Money.From(12m),
      LaborHoursPerOutputUnit = 0.1m,
      PeriodHours = 24,
    });

    var tramp = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    var liner = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));

    var locFrontier = InventoryLocationId.From(builder.NextGuid());
    var locWay = InventoryLocationId.From(builder.NextGuid());
    var locCore = InventoryLocationId.From(builder.NextGuid());
    var locRim = InventoryLocationId.From(builder.NextGuid());

    var hubFrontier = TransportHubId.From(builder.NextGuid());
    var hubWay = TransportHubId.From(builder.NextGuid());
    var hubCore = TransportHubId.From(builder.NextGuid());
    var hubRim = TransportHubId.From(builder.NextGuid());

    var trampHull = VehicleClassId.From(builder.NextGuid());
    var linerHull = VehicleClassId.From(builder.NextGuid());
    var coreFacility = FacilityId.From(builder.NextGuid());

    var oreCat = ProductCategoryId.From(builder.NextGuid());
    var partsCat = ProductCategoryId.From(builder.NextGuid());
    var fuelCat = ProductCategoryId.From(builder.NextGuid());
    var ore = ProductId.From(builder.NextGuid());
    var parts = ProductId.From(builder.NextGuid());
    var fuel = ProductId.From(builder.NextGuid());
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
    var cohortId = ConsumerCohortId.From(builder.NextGuid());
    var area = GeographicAreaId.From(builder.NextGuid());
    var unit = OperatingUnitId.From(builder.NextGuid());

    ProductDefinition Def(ProductId id, ProductCategoryId cat) =>
      new(id, cat, ImmutableArray<ProductInput>.Empty,
        ImmutableArray<ProductAttributeDefinition>.Empty, process, ShelfLife: null);

    // Independent tramp: small hold, tight tank — must bunker at waystations.
    var trampClass = new VehicleClass(
      trampHull,
      CargoCapacity: Quantity.From(30m),
      FuelBurnPerDifficultyHour: 1m,
      CrewLaborPerUnderwayHour: 3m,
      FuelTankCapacity: Quantity.From(5m));

    // Scheduled liner: larger tank, can skip some bunkers on short hops.
    var linerClass = new VehicleClass(
      linerHull,
      CargoCapacity: Quantity.From(50m),
      FuelBurnPerDifficultyHour: 1.2m,
      CrewLaborPerUnderwayHour: 4m,
      FuelTankCapacity: Quantity.From(8m));

    var layout = new FacilityLayout(
      ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
        .Add(unit, new OperatingUnit(unit, OperatingUnitKind.Storage, Quantity.From(100m))),
      ImmutableArray<MaterialRoute>.Empty);

    builder
      .AddProduct(Def(ore, oreCat))
      .AddProduct(Def(parts, partsCat))
      .AddProduct(Def(fuel, fuelCat))
      .AddFirm(tramp, "MV Independent", Money.From(8_000m))
      .AddFirm(liner, "Core Liner Line", Money.From(20_000m))
      .AddFacility(new FacilityBinding(coreFacility, tramp, locCore, locCore, layout))
      .AddHub(new TransportHub(hubFrontier, locFrontier, "Frontier Outpost", DwellHours: 2, BerthCapacity: 2))
      .AddHub(new TransportHub(hubWay, locWay, "Jump Waystation", DwellHours: 1, BerthCapacity: waystationBerths))
      .AddHub(new TransportHub(hubCore, locCore, "Core Port", DwellHours: 2, BerthCapacity: 3))
      .AddHub(new TransportHub(hubRim, locRim, "Sparse Rim Dock", DwellHours: 3, BerthCapacity: 1))
      // Lane network (bidirectional). Difficulty scales fuel; tolls are jump-lane fees.
      .AddCorridor(Lane(builder, hubFrontier, hubWay, hours: 4, difficulty: 1m, toll: 12m))
      .AddCorridor(Lane(builder, hubWay, hubFrontier, hours: 4, difficulty: 1m, toll: 12m))
      .AddCorridor(Lane(builder, hubWay, hubCore, hours: 5, difficulty: 1m, toll: 18m))
      .AddCorridor(Lane(builder, hubCore, hubWay, hours: 5, difficulty: 1m, toll: 18m))
      // Sparse Rim is only on a long direct spoke from the frontier — exceeds tramp tank (5).
      // No staging dock on that jump ⇒ economic scarcity of connectivity for small hulls.
      .AddCorridor(Lane(builder, hubFrontier, hubRim, hours: 12, difficulty: 1m, toll: 8m))
      .AddCorridor(Lane(builder, hubRim, hubFrontier, hours: 12, difficulty: 1m, toll: 8m))
      .AddVehicleClass(trampClass)
      .AddVehicleClass(linerClass)
      .SetTransportFuel(fuel, Money.From(1m))
      .SetLabor(tramp, 24m)
      .SetLabor(liner, 40m)
      .AddCohort(new ConsumerCohort(
        cohortId,
        new PopulationCount(80),
        Money.From(10_000m),
        new PreferenceProfile(
          ImmutableArray.Create(new CategoryPreference(oreCat, 1m)),
          PriceSensitivity: 0.8m,
          QualitySensitivity: 0m,
          BrandLoyalty: 0m),
        area));

    var ids = new TrampIds(
      tramp, liner, locFrontier, locWay, locCore, locRim,
      hubFrontier, hubWay, hubCore, hubRim,
      trampHull, linerHull, coreFacility, ore, parts, fuel);

    return (new EconomySimulation(seed: 77, builder.Build()), ids);
  }

  private static TransportCorridor Lane(
    EconomyWorldBuilder builder,
    TransportHubId from,
    TransportHubId to,
    long hours,
    decimal difficulty,
    decimal toll) =>
    new(
      TransportCorridorId.From(builder.NextGuid()),
      from,
      to,
      TransitHours: hours,
      MaxCargo: Quantity.From(50m),
      Difficulty: difficulty,
      Toll: Money.From(toll));

  private sealed record TrampIds(
    FirmId Tramp,
    FirmId Liner,
    InventoryLocationId LocFrontier,
    InventoryLocationId LocWaystation,
    InventoryLocationId LocCore,
    InventoryLocationId LocRim,
    TransportHubId HubFrontier,
    TransportHubId HubWaystation,
    TransportHubId HubCore,
    TransportHubId HubRim,
    VehicleClassId TrampHull,
    VehicleClassId LinerHull,
    FacilityId CoreFacility,
    ProductId Ore,
    ProductId Parts,
    ProductId Fuel);
}
