using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using TUnit.Core;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class CommodityChainScenarioTests
{
  [Test]
  public async Task VerticallyIntegratedChain_ProducesAndSells_Deterministically()
  {
    var left = CreateSim(seed: 42);
    var right = CreateSim(seed: 42);

    EnqueueStartupCommands(left);
    EnqueueStartupCommands(right);

    var leftResult = await left.AdvanceAsync(SimulationDuration.FromHours(48));
    var rightResult = await right.AdvanceAsync(SimulationDuration.FromHours(48));

    await Assert.That(leftResult.FinalHash).IsEqualTo(rightResult.FinalHash);
    await Assert.That(left.State.Events.OfType<BatchProduced>().Any()).IsTrue();
    await Assert.That(left.State.Events.OfType<GoodsSold>().Any()).IsTrue();

    var firmId = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000001"));
    var cash = left.State.World.Ledgers[firmId].Cash;
    await Assert.That(cash.Amount).IsGreaterThan(0m);
  }

  [Test]
  public async Task Ledger_StaysBalanced_AfterChainRun()
  {
    var sim = CreateSim(seed: 7);
    EnqueueStartupCommands(sim);
    await sim.AdvanceAsync(SimulationDuration.FromHours(24));

    foreach (var ledger in sim.State.World.Ledgers.Values)
    {
      // Assets (cash+inv+ar) - liabilities (ap+wagespayable) - equity - revenue + expenses ≈ 0
      // With our signing convention (debit +, credit -), sum of all balances should be ~0.
      var sum = Enum.GetValues<Novolis.Economy.Accounting.AccountRole>()
        .Sum(r => ledger.Balance(r).Amount);
      await Assert.That(Math.Abs(sum)).IsLessThan(0.01m);
    }
  }

  private static EconomySimulation CreateSim(ulong seed)
  {
    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      WageRatePerHour = Money.From(5m),
      LaborHoursPerOutputUnit = 0.05m,
      PeriodHours = 24,
      EnableSpoilage = false,
    });

    var firm = FirmId.From(builder.NextGuid());
    var facility = FacilityId.From(builder.NextGuid());
    var storage = InventoryLocationId.From(builder.NextGuid());
    var retail = InventoryLocationId.From(builder.NextGuid());
    var rawCat = ProductCategoryId.From(builder.NextGuid());
    var midCat = ProductCategoryId.From(builder.NextGuid());
    var finCat = ProductCategoryId.From(builder.NextGuid());
    var raw = ProductId.From(builder.NextGuid());
    var mid = ProductId.From(builder.NextGuid());
    var fin = ProductId.From(builder.NextGuid());
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
    var mfg = OperatingUnitId.From(builder.NextGuid());
    var routeId = FreightRouteId.From(builder.NextGuid());
    var cohortId = ConsumerCohortId.From(builder.NextGuid());
    var area = GeographicAreaId.From(builder.NextGuid());

    // Pin firm guid used in asserts
    firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000001"));

    var rawDef = new ProductDefinition(
      raw, rawCat, ImmutableArray<ProductInput>.Empty,
      ImmutableArray<ProductAttributeDefinition>.Empty, process, ShelfLife: null);
    var midDef = new ProductDefinition(
      mid, midCat,
      ImmutableArray.Create(new ProductInput(raw, Quantity.From(1m))),
      ImmutableArray<ProductAttributeDefinition>.Empty, process, ShelfLife: null);
    var finDef = new ProductDefinition(
      fin, finCat,
      ImmutableArray.Create(new ProductInput(mid, Quantity.From(1m))),
      ImmutableArray<ProductAttributeDefinition>.Empty, process, ShelfLife: null);

    var layout = new FacilityLayout(
      ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
        .Add(mfg, new OperatingUnit(mfg, OperatingUnitKind.Manufacturing, Quantity.From(100m))),
      ImmutableArray<MaterialRoute>.Empty);

    builder
      .AddProduct(rawDef)
      .AddProduct(midDef)
      .AddProduct(finDef)
      .AddFirm(firm, "Integrated Co", Money.From(10_000m))
      .AddFacility(new FacilityBinding(facility, firm, storage, retail, layout))
      .AddInventory(firm, storage, new ProductBatch(
        raw, Quantity.From(500m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null))
      .AddRoute(new FreightRoute(routeId, storage, retail, TransitHours: 1, Capacity: Quantity.From(50m)))
      .SetRestockRoute(facility, routeId)
      .SetLabor(firm, 40m)
      .AddCohort(new ConsumerCohort(
        cohortId,
        new PopulationCount(100),
        Money.From(5_000m),
        new PreferenceProfile(
          ImmutableArray.Create(new CategoryPreference(finCat, 1m)),
          PriceSensitivity: 1m,
          QualitySensitivity: 0m,
          BrandLoyalty: 0m),
        area));

    var world = builder.Build();
    // Stash ids on world via production plans keys later using commands
    world.Products[raw] = rawDef;
    // Attach scenario ids for commands via a side channel: use well-known facility/product from world
    ScenarioIds.Facility = facility;
    ScenarioIds.Firm = firm;
    ScenarioIds.Raw = raw;
    ScenarioIds.Mid = mid;
    ScenarioIds.Fin = fin;
    return new EconomySimulation(seed, world);
  }

  private static void EnqueueStartupCommands(EconomySimulation sim)
  {
    sim.Enqueue(new SetProductionPlan(ScenarioIds.Firm, ScenarioIds.Facility, ScenarioIds.Mid, Quantity.From(10m)));
    sim.Enqueue(new SetProductionPlan(ScenarioIds.Firm, ScenarioIds.Facility, ScenarioIds.Fin, Quantity.From(8m)));
    sim.Enqueue(new SetRetailPrice(ScenarioIds.Firm, ScenarioIds.Facility, ScenarioIds.Fin, Money.From(5m)));
  }

  private static class ScenarioIds
  {
    public static FirmId Firm;
    public static FacilityId Facility;
    public static ProductId Raw;
    public static ProductId Mid;
    public static ProductId Fin;
  }
}
