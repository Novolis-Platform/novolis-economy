using System.Collections.Immutable;
using System.Diagnostics;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Logistics;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using TUnit.Core;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class HubNetworkScenarioTests
{
  [Test]
  public async Task HubNetwork_DeliversViaTransfer_BurnsFuelAndPaysToll()
  {
    var sim = CreateHubSim();
    var ids = HubIds.Current;
    sim.Enqueue(new PlanShipment(
      ids.Firm,
      ids.HubNorth.Value,
      ids.HubSouth.Value,
      ids.Cargo,
      Quantity.From(20m),
      ids.Vehicle.Value));

    await sim.AdvanceAsync(SimulationDuration.FromHours(80));

    var world = sim.State.World;
    var delivered = world.Inventory.GetQuantity(new InventoryKey(ids.Firm, ids.LocSouth, ids.Cargo));
    await Assert.That(delivered.Value).IsEqualTo(20m);
    await Assert.That(world.TransportStats.FuelBurned.Value).IsGreaterThan(0m);
    await Assert.That(world.TransportStats.TollsPaid.Amount).IsGreaterThan(0m);
    await Assert.That(world.TransportStats.CrewLaborHours).IsGreaterThan(0m);
    await Assert.That(sim.State.Events.OfType<ShipmentHubArrived>().Any()).IsTrue();
    await Assert.That(sim.State.Events.OfType<ShipmentLegStarted>().Any()).IsTrue();
    await Assert.That(sim.State.Events.OfType<ShipmentDelivered>().Any()).IsTrue();

    var ledger = world.Ledgers[ids.Firm];
    var sum = Enum.GetValues<AccountRole>().Sum(r => ledger.Balance(r).Amount);
    await Assert.That(Math.Abs(sum)).IsLessThan(0.01m);
  }

  [Test]
  public async Task HubNetwork_IsDeterministic()
  {
    var left = CreateHubSim();
    var right = CreateHubSim();
    var ids = HubIds.Current;
    var plan = new PlanShipment(
      ids.Firm, ids.HubNorth.Value, ids.HubSouth.Value, ids.Cargo, Quantity.From(20m), ids.Vehicle.Value);
    left.Enqueue(plan);
    right.Enqueue(plan);

    var a = await left.AdvanceAsync(SimulationDuration.FromHours(80));
    var b = await right.AdvanceAsync(SimulationDuration.FromHours(80));
    await Assert.That(a.FinalHash).IsEqualTo(b.FinalHash);
  }

  [Test]
  public async Task HubNetwork_FailsWhenFuelStockoutAtOrigin()
  {
    var sim = CreateHubSim(fuelAtNorth: 0m, fuelAtTransfer: 100m);
    var ids = HubIds.Current;
    sim.Enqueue(new PlanShipment(
      ids.Firm, ids.HubNorth.Value, ids.HubSouth.Value, ids.Cargo, Quantity.From(20m), ids.Vehicle.Value));

    await sim.AdvanceAsync(SimulationDuration.FromHours(5));

    await Assert.That(sim.State.World.TransportStats.FailedPlans).IsGreaterThan(0);
    await Assert.That(sim.State.Events.OfType<ShipmentPlanFailed>().Any()).IsTrue();
    var atSouth = sim.State.World.Inventory.GetQuantity(new InventoryKey(ids.Firm, ids.LocSouth, ids.Cargo));
    await Assert.That(atSouth.Value).IsEqualTo(0m);
  }

  [Test]
  public async Task ItineraryPlanner_RejectsLegExceedingTank()
  {
    var vehicle = new VehicleClass(
      VehicleClassId.From(Guid.Parse("00000000-0000-4000-8000-0000000000aa")),
      CargoCapacity: Quantity.From(100m),
      FuelBurnPerDifficultyHour: 1m,
      CrewLaborPerUnderwayHour: 1m,
      FuelTankCapacity: Quantity.From(5m));

    var hubA = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    var hubB = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
    var longCorridor = new TransportCorridor(
      TransportCorridorId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1")),
      hubA,
      hubB,
      TransitHours: 10,
      MaxCargo: Quantity.From(100m),
      Difficulty: 1m,
      Toll: Money.Zero);

    var ok = ItineraryPlanner.TryPlan(
      hubA,
      hubB,
      Quantity.From(10m),
      vehicle,
      new Dictionary<TransportCorridorId, TransportCorridor> { [longCorridor.Id] = longCorridor },
      out _);
    await Assert.That(ok).IsFalse();
  }

  [Test]
  public async Task HubNetwork_MachineSpeedSmoke_PrintsAggregates()
  {
    var sw = Stopwatch.StartNew();
    var sim = CreateHubSim();
    var ids = HubIds.Current;
    sim.Enqueue(new PlanShipment(
      ids.Firm, ids.HubNorth.Value, ids.HubSouth.Value, ids.Cargo, Quantity.From(20m), ids.Vehicle.Value));

    // Second wave after first should deliver
    for (var i = 0; i < 200; i++)
    {
      await sim.AdvanceAsync(SimulationDuration.FromHours(1));
      if (i == 100)
      {
        // Replenish fuel and cargo for a second shipment attempt
        var world = sim.State.World;
        world.Inventory.Add(
          new InventoryKey(ids.Firm, ids.LocNorth, ids.Cargo),
          new ProductBatch(ids.Cargo, Quantity.From(20m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
        world.Inventory.Add(
          new InventoryKey(ids.Firm, ids.LocNorth, ids.Fuel),
          new ProductBatch(ids.Fuel, Quantity.From(50m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
        world.Inventory.Add(
          new InventoryKey(ids.Firm, ids.LocTransfer, ids.Fuel),
          new ProductBatch(ids.Fuel, Quantity.From(50m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
        sim.Enqueue(new PlanShipment(
          ids.Firm, ids.HubNorth.Value, ids.HubSouth.Value, ids.Cargo, Quantity.From(20m), ids.Vehicle.Value));
      }
    }

    sw.Stop();
    var stats = sim.State.World.TransportStats;
    var avgTransit = stats.TransitSampleCount == 0
      ? 0.0
      : (double)stats.TransitHoursSum / stats.TransitSampleCount;
    var ledger = sim.State.World.Ledgers[ids.Firm];
    var hash = sim.State.Hash;

    Console.WriteLine(
      $"hub-network smoke: {sw.ElapsedMilliseconds}ms cargo={stats.CargoDelivered.Value} " +
      $"fuelBurned={stats.FuelBurned.Value} bunkered={stats.FuelBunkered.Value} " +
      $"tolls={stats.TollsPaid.Amount} crew={stats.CrewLaborHours} " +
      $"avgTransit={avgTransit:F1}h failedPlans={stats.FailedPlans} " +
      $"cash={ledger.Cash.Amount} hash={hash:X16}");

    await Assert.That(stats.CargoDelivered.Value).IsGreaterThan(19.9m);
    await Assert.That(sw.ElapsedMilliseconds).IsLessThan(5_000);
  }

  private static EconomySimulation CreateHubSim(decimal fuelAtNorth = 50m, decimal fuelAtTransfer = 50m)
  {
    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      WageRatePerHour = Money.From(8m),
      LaborHoursPerOutputUnit = 0.1m,
      PeriodHours = 24,
    });

    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000001"));
    var locNorth = InventoryLocationId.From(builder.NextGuid());
    var locTransfer = InventoryLocationId.From(builder.NextGuid());
    var locSouth = InventoryLocationId.From(builder.NextGuid());
    var hubNorth = TransportHubId.From(builder.NextGuid());
    var hubTransfer = TransportHubId.From(builder.NextGuid());
    var hubSouth = TransportHubId.From(builder.NextGuid());
    var corridorNT = TransportCorridorId.From(builder.NextGuid());
    var corridorTS = TransportCorridorId.From(builder.NextGuid());
    var corridorDirect = TransportCorridorId.From(builder.NextGuid());
    var vehicleId = VehicleClassId.From(builder.NextGuid());
    var cargoCat = ProductCategoryId.From(builder.NextGuid());
    var fuelCat = ProductCategoryId.From(builder.NextGuid());
    var cargo = ProductId.From(builder.NextGuid());
    var fuel = ProductId.From(builder.NextGuid());
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));

    var cargoDef = new ProductDefinition(
      cargo, cargoCat, ImmutableArray<ProductInput>.Empty,
      ImmutableArray<ProductAttributeDefinition>.Empty, process, ShelfLife: null);
    var fuelDef = new ProductDefinition(
      fuel, fuelCat, ImmutableArray<ProductInput>.Empty,
      ImmutableArray<ProductAttributeDefinition>.Empty, process, ShelfLife: null);

    var vehicle = new VehicleClass(
      vehicleId,
      CargoCapacity: Quantity.From(40m),
      FuelBurnPerDifficultyHour: 1m,
      CrewLaborPerUnderwayHour: 2m,
      FuelTankCapacity: Quantity.From(5m));

    builder
      .AddProduct(cargoDef)
      .AddProduct(fuelDef)
      .AddFirm(firm, "Carrier Co", Money.From(5_000m))
      .AddHub(new TransportHub(hubNorth, locNorth, "Hub North", DwellHours: 2, BerthCapacity: 2))
      .AddHub(new TransportHub(hubTransfer, locTransfer, "Transfer", DwellHours: 1, BerthCapacity: 1))
      .AddHub(new TransportHub(hubSouth, locSouth, "Hub South", DwellHours: 2, BerthCapacity: 2))
      .AddCorridor(new TransportCorridor(
        corridorNT, hubNorth, hubTransfer, TransitHours: 4, MaxCargo: Quantity.From(40m),
        Difficulty: 1m, Toll: Money.From(10m)))
      .AddCorridor(new TransportCorridor(
        corridorTS, hubTransfer, hubSouth, TransitHours: 5, MaxCargo: Quantity.From(40m),
        Difficulty: 1m, Toll: Money.From(15m)))
      // Long direct exceeds tank (10 burn > 5 capacity) — planner must use transfer path.
      .AddCorridor(new TransportCorridor(
        corridorDirect, hubNorth, hubSouth, TransitHours: 10, MaxCargo: Quantity.From(40m),
        Difficulty: 1m, Toll: Money.From(5m)))
      .AddVehicleClass(vehicle)
      .SetTransportFuel(fuel, Money.From(1m))
      .SetLabor(firm, 20m)
      .AddInventory(firm, locNorth, new ProductBatch(
        cargo, Quantity.From(20m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));

    if (fuelAtNorth > 0m)
    {
      builder.AddInventory(firm, locNorth, new ProductBatch(
        fuel, Quantity.From(fuelAtNorth), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
    }

    if (fuelAtTransfer > 0m)
    {
      builder.AddInventory(firm, locTransfer, new ProductBatch(
        fuel, Quantity.From(fuelAtTransfer), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
    }

    HubIds.Current = new HubIds(
      firm, locNorth, locTransfer, locSouth, hubNorth, hubTransfer, hubSouth, vehicleId, cargo, fuel);

    return new EconomySimulation(seed: 42, builder.Build());
  }

  private sealed record HubIds(
    FirmId Firm,
    InventoryLocationId LocNorth,
    InventoryLocationId LocTransfer,
    InventoryLocationId LocSouth,
    TransportHubId HubNorth,
    TransportHubId HubTransfer,
    TransportHubId HubSouth,
    VehicleClassId Vehicle,
    ProductId Cargo,
    ProductId Fuel)
  {
    public static HubIds Current { get; set; } = null!;
  }
}
