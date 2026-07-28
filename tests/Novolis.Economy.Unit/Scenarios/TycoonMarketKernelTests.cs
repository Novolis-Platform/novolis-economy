using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using TUnit.Core;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class TycoonMarketKernelTests
{
  [Test]
  public async Task HubOrders_MatchWhenPricesCross_PartialFill()
  {
    var builder = new EconomyWorldBuilder(new EconomyPolicy { PeriodHours = 10_000 });
    var seller = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
    var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d2"));
    var loc = InventoryLocationId.From(builder.NextGuid());
    var cat = ProductCategoryId.From(builder.NextGuid());
    var goods = ProductId.From(builder.NextGuid());
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
    var def = new ProductDefinition(
      goods, cat, ImmutableArray<ProductInput>.Empty,
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null);

    builder
      .AddProduct(def)
      .AddFirm(seller, "Seller", Money.From(100m))
      .AddFirm(buyer, "Buyer", Money.From(1_000m))
      .AddInventory(seller, loc, new ProductBatch(
        goods, Quantity.From(10m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));

    var sim = new EconomySimulation(3, builder.Build());
    sim.Enqueue(new PostHubOrder(seller, loc, goods, HubOrderSide.Sell, Quantity.From(10m), Money.From(5m)));
    sim.Enqueue(new PostHubOrder(buyer, loc, goods, HubOrderSide.Buy, Quantity.From(4m), Money.From(6m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    var fills = sim.State.Events.OfType<HubOrderFilled>().ToList();
    await Assert.That(fills.Count).IsEqualTo(1);
    await Assert.That(fills[0].Quantity.Value).IsEqualTo(4m);
    await Assert.That(sim.State.World.Inventory.GetQuantity(new InventoryKey(buyer, loc, goods)).Value)
      .IsEqualTo(4m);
    await Assert.That(sim.State.World.Inventory.GetQuantity(new InventoryKey(seller, loc, goods)).Value)
      .IsEqualTo(6m);
    await Assert.That(sim.State.World.HubOrders.Count(o => !o.IsFilled)).IsEqualTo(1);
  }

  [Test]
  public async Task HubOrders_NoMatchWhenBuyBelowSell()
  {
    var builder = new EconomyWorldBuilder(new EconomyPolicy { PeriodHours = 10_000 });
    var seller = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
    var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d2"));
    var loc = InventoryLocationId.From(builder.NextGuid());
    var cat = ProductCategoryId.From(builder.NextGuid());
    var goods = ProductId.From(builder.NextGuid());
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
    var def = new ProductDefinition(
      goods, cat, ImmutableArray<ProductInput>.Empty,
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null);

    builder
      .AddProduct(def)
      .AddFirm(seller, "Seller", Money.From(100m))
      .AddFirm(buyer, "Buyer", Money.From(1_000m))
      .AddInventory(seller, loc, new ProductBatch(
        goods, Quantity.From(10m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));

    var sim = new EconomySimulation(4, builder.Build());
    sim.Enqueue(new PostHubOrder(seller, loc, goods, HubOrderSide.Sell, Quantity.From(10m), Money.From(8m)));
    sim.Enqueue(new PostHubOrder(buyer, loc, goods, HubOrderSide.Buy, Quantity.From(10m), Money.From(5m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    await Assert.That(sim.State.Events.OfType<HubOrderFilled>().Any()).IsFalse();
    await Assert.That(sim.State.World.HubOrders.Count).IsEqualTo(2);
  }

  [Test]
  public async Task ProductionThrottle_TapersThenFloors()
  {
    await Assert.That(ProductionThrottle.Rate(10m, onHand: 0m, targetOnHand: 100m)).IsEqualTo(10m);
    await Assert.That(ProductionThrottle.Rate(10m, onHand: 70m, targetOnHand: 100m)).IsEqualTo(10m);
    var mid = ProductionThrottle.Rate(10m, onHand: 85m, targetOnHand: 100m);
    await Assert.That(mid).IsLessThan(10m);
    await Assert.That(mid).IsGreaterThan(0m);
    await Assert.That(ProductionThrottle.Rate(10m, onHand: 100m, targetOnHand: 100m)).IsEqualTo(0m);
  }

  [Test]
  public async Task MoneyStock_SumsCashAndHouseholds()
  {
    var builder = new EconomyWorldBuilder();
    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e1"));
    var cat = ProductCategoryId.From(builder.NextGuid());
    var area = GeographicAreaId.From(builder.NextGuid());
    builder
      .AddFirm(firm, "F", Money.From(100m))
      .AddCohort(new ConsumerCohort(
        ConsumerCohortId.From(builder.NextGuid()),
        new PopulationCount(1),
        Money.From(40m),
        new PreferenceProfile(ImmutableArray<CategoryPreference>.Empty, 1m, 0m, 0m),
        area));
    var world = builder.Build();
    await Assert.That(MoneyStock.Liquid(world)).IsEqualTo(140m);
  }
}
