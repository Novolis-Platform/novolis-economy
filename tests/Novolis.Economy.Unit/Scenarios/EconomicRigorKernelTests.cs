using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using TUnit.Core;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class EconomicRigorKernelTests
{
  [Test]
  public async Task AreaDemand_CohortCannotBuyOutsideItsArea()
  {
    var areaA = GeographicAreaId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    var areaB = GeographicAreaId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
    var builder = new EconomyWorldBuilder(new EconomyPolicy { PeriodHours = 10_000 });

    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000001"));
    var facA = FacilityId.From(builder.NextGuid());
    var facB = FacilityId.From(builder.NextGuid());
    var locA = InventoryLocationId.From(builder.NextGuid());
    var locB = InventoryLocationId.From(builder.NextGuid());
    var cat = ProductCategoryId.From(builder.NextGuid());
    var goods = ProductId.From(builder.NextGuid());
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
    var cohortId = ConsumerCohortId.From(builder.NextGuid());
    var mfg = OperatingUnitId.From(builder.NextGuid());

    var def = new ProductDefinition(
      goods, cat, ImmutableArray<ProductInput>.Empty,
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null);
    var layout = new FacilityLayout(
      ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
        .Add(mfg, new OperatingUnit(mfg, OperatingUnitKind.Sales, Quantity.From(10m))),
      ImmutableArray<MaterialRoute>.Empty);

    builder
      .AddProduct(def)
      .AddFirm(firm, "Retailer", Money.From(1_000m))
      .AddFacility(new FacilityBinding(facA, firm, locA, locA, layout, areaA))
      .AddFacility(new FacilityBinding(facB, firm, locB, locB, layout, areaB))
      .AddInventory(firm, locB, new ProductBatch(
        goods, Quantity.From(50m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null))
      .AddCohort(new ConsumerCohort(
        cohortId,
        new PopulationCount(10),
        Money.From(500m),
        new PreferenceProfile(
          ImmutableArray.Create(new CategoryPreference(cat, 1m)),
          1m, 0m, 0m),
        areaA));

    var sim = new EconomySimulation(7, builder.Build());
    sim.Enqueue(new SetRetailPrice(firm, facB, goods, Money.From(5m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    await Assert.That(sim.State.Events.OfType<GoodsSold>().Any()).IsFalse();
    await Assert.That(sim.State.World.Inventory.GetQuantity(new InventoryKey(firm, locB, goods)).Value)
      .IsEqualTo(50m);
    await Assert.That(sim.State.World.Cohorts[0].BudgetRemaining.Amount).IsEqualTo(500m);
  }

  [Test]
  public async Task AreaDemand_CohortBuysWhenAreasMatch()
  {
    var areaA = GeographicAreaId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    var builder = new EconomyWorldBuilder(new EconomyPolicy { PeriodHours = 10_000 });

    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000001"));
    var facA = FacilityId.From(builder.NextGuid());
    var locA = InventoryLocationId.From(builder.NextGuid());
    var cat = ProductCategoryId.From(builder.NextGuid());
    var goods = ProductId.From(builder.NextGuid());
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
    var cohortId = ConsumerCohortId.From(builder.NextGuid());
    var mfg = OperatingUnitId.From(builder.NextGuid());

    var def = new ProductDefinition(
      goods, cat, ImmutableArray<ProductInput>.Empty,
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null);
    var layout = new FacilityLayout(
      ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
        .Add(mfg, new OperatingUnit(mfg, OperatingUnitKind.Sales, Quantity.From(10m))),
      ImmutableArray<MaterialRoute>.Empty);

    builder
      .AddProduct(def)
      .AddFirm(firm, "Retailer", Money.From(1_000m))
      .AddFacility(new FacilityBinding(facA, firm, locA, locA, layout, areaA))
      .AddInventory(firm, locA, new ProductBatch(
        goods, Quantity.From(50m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null))
      .AddCohort(new ConsumerCohort(
        cohortId,
        new PopulationCount(10),
        Money.From(500m),
        new PreferenceProfile(
          ImmutableArray.Create(new CategoryPreference(cat, 1m)),
          1m, 0m, 0m),
        areaA));

    var sim = new EconomySimulation(8, builder.Build());
    sim.Enqueue(new SetRetailPrice(firm, facA, goods, Money.From(5m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    await Assert.That(sim.State.Events.OfType<GoodsSold>().Any()).IsTrue();
    await Assert.That(sim.State.World.Cohorts[0].BudgetRemaining.Amount).IsLessThan(500m);
  }

  [Test]
  public async Task HaulCostEstimator_IsDeterministicForKnownCorridor()
  {
    var corridorId = TransportCorridorId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
    var hubA = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    var hubB = TransportHubId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
    var corridor = new TransportCorridor(
      corridorId, hubA, hubB, TransitHours: 10, MaxCargo: Quantity.From(40m),
      Difficulty: 2m, Toll: Money.From(15m));
    var vehicle = new VehicleClass(
      VehicleClassId.From(Guid.Parse("00000000-0000-4000-8000-0000000000aa")),
      CargoCapacity: Quantity.From(40m),
      FuelBurnPerDifficultyHour: 0.5m,
      CrewLaborPerUnderwayHour: 1m,
      FuelTankCapacity: Quantity.From(20m));

    var estimate = HaulCostEstimator.Estimate(
      HaulCostEstimator.SingleLeg(corridorId),
      new Dictionary<TransportCorridorId, TransportCorridor> { [corridorId] = corridor },
      vehicle,
      Money.From(3m),
      Money.From(2m));

    // fuel = 10 * 2 * 0.5 = 10; fuelCost = 20; crew = 10 * 1 * 3 = 30; toll = 15 → 65
    await Assert.That(estimate.UnderwayHours).IsEqualTo(10);
    await Assert.That(estimate.FuelUnits).IsEqualTo(10m);
    await Assert.That(estimate.Tolls.Amount).IsEqualTo(15m);
    await Assert.That(estimate.CrewCost.Amount).IsEqualTo(30m);
    await Assert.That(estimate.FuelCost.Amount).IsEqualTo(20m);
    await Assert.That(estimate.TotalVariableCost.Amount).IsEqualTo(65m);
  }

  [Test]
  public async Task InventoryPressurePricing_ScarcePremium_AbundantDiscount_Clamps()
  {
    var basePrice = Money.From(10m);
    var scarce = InventoryPressurePricing.Adjust(basePrice, onHand: 0m, targetOnHand: 20m, maxPremium: 0.25m, maxDiscount: 0.25m);
    var abundant = InventoryPressurePricing.Adjust(basePrice, onHand: 100m, targetOnHand: 20m, maxPremium: 0.25m, maxDiscount: 0.25m);
    var atTarget = InventoryPressurePricing.Adjust(basePrice, onHand: 20m, targetOnHand: 20m);

    await Assert.That(scarce.Amount).IsEqualTo(12.5m);
    await Assert.That(abundant.Amount).IsEqualTo(7.5m);
    await Assert.That(atTarget.Amount).IsEqualTo(10m);
  }
}
