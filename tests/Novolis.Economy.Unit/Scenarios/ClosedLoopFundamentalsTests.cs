using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Logistics;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using TUnit.Core;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class ClosedLoopFundamentalsTests
{
  [Test]
  public async Task ClosedLoopCredits_WagesRaiseCohortBudget_NoPeriodRemint()
  {
    var sim = CreateClosedLoopSim(periodHours: 4);
    var ids = ClosedLoopIds.Current;
    var world = sim.State.World;
    var wage = Money.From(100m);
    var budgetBefore = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    var cashBefore = world.Ledgers[ids.Firm].Cash.Amount;

    LedgerEngine.AccrueWages(world.Ledgers[ids.Firm], wage, SimulationDate.Epoch);
    world.AccruedWages[ids.Firm] = wage;
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    var wages = sim.State.Events.OfType<WagesPaid>().Sum(e => e.Amount.Amount);
    var credits = sim.State.Events.OfType<HouseholdCreditsIssued>().Sum(e => e.Amount.Amount);
    await Assert.That(wages).IsEqualTo(100m);
    await Assert.That(credits).IsEqualTo(100m);

    var budgetAfterWage = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    await Assert.That(budgetAfterWage).IsEqualTo(budgetBefore + wages);
    await Assert.That(world.Ledgers[ids.Firm].Cash.Amount).IsEqualTo(cashBefore - wages);

    // Cross a period boundary without reminting (CarryForward).
    await sim.AdvanceAsync(SimulationDuration.FromHours(4));
    var budgetAfterPeriod = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    await Assert.That(budgetAfterPeriod).IsEqualTo(budgetAfterWage);
    await Assert.That(sim.State.Events.OfType<AccountingPeriodClosed>().Any()).IsTrue();
  }

  [Test]
  public async Task InterFirmTransfer_MovesInventoryAndCash_FailsWhenBuyerBroke()
  {
    var sim = CreateClosedLoopSim();
    var ids = ClosedLoopIds.Current;
    var world = sim.State.World;

    sim.Enqueue(new TransferGoodsForCash(
      ids.Firm,
      ids.Buyer,
      ids.Location,
      ids.Goods,
      Quantity.From(10m),
      Money.From(5m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    await Assert.That(sim.State.Events.OfType<GoodsSoldInterFirm>().Count()).IsEqualTo(1);
    await Assert.That(world.Inventory.GetQuantity(new InventoryKey(ids.Firm, ids.Location, ids.Goods)).Value)
      .IsEqualTo(90m);
    await Assert.That(world.Inventory.GetQuantity(new InventoryKey(ids.Buyer, ids.Location, ids.Goods)).Value)
      .IsEqualTo(10m);
    await Assert.That(world.Ledgers[ids.Firm].Cash.Amount).IsEqualTo(10_000m + 50m);
    await Assert.That(world.Ledgers[ids.Buyer].Cash.Amount).IsEqualTo(200m - 50m);

    foreach (var ledger in world.Ledgers.Values)
    {
      var sum = Enum.GetValues<AccountRole>().Sum(r => ledger.Balance(r).Amount);
      await Assert.That(Math.Abs(sum)).IsLessThan(0.01m);
    }

    // Buyer has 150 left — cannot afford 200.
    sim.Enqueue(new TransferGoodsForCash(
      ids.Firm,
      ids.Buyer,
      ids.Location,
      ids.Goods,
      Quantity.From(40m),
      Money.From(5m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    var fail = sim.State.Events.OfType<TransferGoodsFailed>().LastOrDefault();
    await Assert.That(fail).IsNotNull();
    await Assert.That(fail!.Reason).IsEqualTo("cash");
    await Assert.That(world.Inventory.GetQuantity(new InventoryKey(ids.Firm, ids.Location, ids.Goods)).Value)
      .IsEqualTo(90m);
  }

  [Test]
  public async Task TollTreasury_ConservesPayerPlusBeneficiaryCash()
  {
    var sim = CreateTollTreasurySim();
    var ids = TollIds.Current;
    var world = sim.State.World;
    var liquidBefore =
      world.Ledgers[ids.Shipper].Cash.Amount + world.Ledgers[ids.Treasury].Cash.Amount;

    sim.Enqueue(new PlanShipment(
      ids.Shipper,
      ids.HubA.Value,
      ids.HubB.Value,
      ids.Cargo,
      Quantity.From(10m),
      ids.Vehicle.Value));

    await sim.AdvanceAsync(SimulationDuration.FromHours(40));

    await Assert.That(world.TransportStats.TollsPaid.Amount).IsGreaterThan(0m);
    var liquidAfter =
      world.Ledgers[ids.Shipper].Cash.Amount + world.Ledgers[ids.Treasury].Cash.Amount;
    // Toll moves cash shipper → treasury; fuel burn is inventory COGS (not liquid cash).
    await Assert.That(liquidAfter).IsEqualTo(liquidBefore);
    await Assert.That(world.Ledgers[ids.Treasury].Cash.Amount)
      .IsEqualTo(world.TransportStats.TollsPaid.Amount);
  }

  private static EconomySimulation CreateClosedLoopSim(int periodHours = 24)
  {
    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      WageRatePerHour = Money.From(10m),
      LaborHoursPerOutputUnit = 0.1m,
      PeriodHours = periodHours,
      HouseholdCreditFromWages = true,
      CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
    });

    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
    var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b2"));
    var location = InventoryLocationId.From(builder.NextGuid());
    var cat = ProductCategoryId.From(builder.NextGuid());
    var goods = ProductId.From(builder.NextGuid());
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
    var cohortId = ConsumerCohortId.From(builder.NextGuid());
    var area = GeographicAreaId.From(builder.NextGuid());

    var goodsDef = new ProductDefinition(
      goods, cat, ImmutableArray<ProductInput>.Empty,
      ImmutableArray<ProductAttributeDefinition>.Empty, process, ShelfLife: null);

    builder
      .AddProduct(goodsDef)
      .AddFirm(firm, "Producer", Money.From(10_000m))
      .AddFirm(buyer, "Buyer", Money.From(200m))
      .AddInventory(firm, location, new ProductBatch(
        goods, Quantity.From(100m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null))
      .SetLabor(firm, 0m)
      .AddCohort(new ConsumerCohort(
        cohortId,
        new PopulationCount(100),
        Money.From(1_000m),
        new PreferenceProfile(
          ImmutableArray.Create(new CategoryPreference(cat, 1m)),
          PriceSensitivity: 1m,
          QualitySensitivity: 0m,
          BrandLoyalty: 0m),
        area));

    ClosedLoopIds.Current = new ClosedLoopIds(firm, buyer, location, goods);
    return new EconomySimulation(11, builder.Build());
  }

  private static EconomySimulation CreateTollTreasurySim()
  {
    var treasury = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
    var shipper = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      WageRatePerHour = Money.From(1m),
      PeriodHours = 10_000,
      TollBeneficiaryFirmId = treasury,
    });

    var locA = InventoryLocationId.From(builder.NextGuid());
    var locB = InventoryLocationId.From(builder.NextGuid());
    var hubA = TransportHubId.From(builder.NextGuid());
    var hubB = TransportHubId.From(builder.NextGuid());
    var corridor = TransportCorridorId.From(builder.NextGuid());
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
      FuelBurnPerDifficultyHour: 0.1m,
      CrewLaborPerUnderwayHour: 0m,
      FuelTankCapacity: Quantity.From(20m));

    builder
      .AddProduct(cargoDef)
      .AddProduct(fuelDef)
      .AddFirm(treasury, "Treasury", Money.Zero)
      .AddFirm(shipper, "Shipper", Money.From(5_000m))
      .AddHub(new TransportHub(hubA, locA, "Hub A", DwellHours: 1, BerthCapacity: 2))
      .AddHub(new TransportHub(hubB, locB, "Hub B", DwellHours: 1, BerthCapacity: 2))
      .AddCorridor(new TransportCorridor(
        corridor, hubA, hubB, TransitHours: 5, MaxCargo: Quantity.From(40m),
        Difficulty: 1m, Toll: Money.From(25m)))
      .AddVehicleClass(vehicle)
      .SetTransportFuel(fuel, Money.From(1m))
      .AddInventory(shipper, locA, new ProductBatch(
        cargo, Quantity.From(10m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null))
      .AddInventory(shipper, locA, new ProductBatch(
        fuel, Quantity.From(50m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null))
      .SetLabor(shipper, 0m);

    TollIds.Current = new TollIds(shipper, treasury, hubA, hubB, cargo, vehicleId);
    return new EconomySimulation(22, builder.Build());
  }

  private sealed record ClosedLoopIds(
    FirmId Firm,
    FirmId Buyer,
    InventoryLocationId Location,
    ProductId Goods)
  {
    public static ClosedLoopIds Current { get; set; } = null!;
  }

  private sealed record TollIds(
    FirmId Shipper,
    FirmId Treasury,
    TransportHubId HubA,
    TransportHubId HubB,
    ProductId Cargo,
    VehicleClassId Vehicle)
  {
    public static TollIds Current { get; set; } = null!;
  }
}
