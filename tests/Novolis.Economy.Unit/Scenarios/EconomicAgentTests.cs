using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class EconomicAgentTests
{
  [Test]
  public async Task ExtractiveAgent_PostsSell_ThatFillsAgainstBuyer()
  {
    var seller = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
    var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
    var ore = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
    var parts = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
    var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
    var fac = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e1"));
    var unit = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f1"));
    var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));

    var builder = new EconomyWorldBuilder(new EconomyPolicy());
    builder.AddProduct(new ProductDefinition(
      ore, cat, ImmutableArray.Create(new ProductInput(parts, Quantity.From(0.1m))),
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
    builder.AddProduct(new ProductDefinition(
      parts, cat, ImmutableArray<ProductInput>.Empty,
      ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
    builder.AddFirm(seller, "Mine", Money.From(5_000m));
    builder.AddFirm(buyer, "Plant", Money.From(5_000m));
    builder.AddFacility(new FacilityBinding(
      fac, seller, loc, loc,
      new FacilityLayout(
        ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
          unit, new OperatingUnit(unit, OperatingUnitKind.Manufacturing, Quantity.From(80m))),
        ImmutableArray<MaterialRoute>.Empty)));
    var world = builder.Build();
    world.Inventory.Add(
      new InventoryKey(seller, loc, ore),
      new ProductBatch(ore, Quantity.From(40m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
    world.Inventory.Add(
      new InventoryKey(seller, loc, parts),
      new ProductBatch(parts, Quantity.From(20m), new ProductQuality(100m), Money.From(4m), SimulationDate.Epoch, null));
    var sim = new EconomySimulation(11, world);

    var agent = new ExtractiveFirmAgent(seller, new ExtractiveFirmAgentPolicy(
      [new AgentSite(loc, fac, Name: "mine")],
      ore, parts, BaseOutputRate: 2m, OutputCap: 80m, InputPerOutput: 0.1m, InputFloor: 8m,
      SellAboveStock: 10m, SellKeepFloor: 5m, SellMaxQty: 20m,
      OutputGatePrice: 2m, InputLimitPrice: 7m));
    var ctx = new AgentContext(sim, new DeterministicRandom(11));
    agent.Tick(ctx);
    sim.Enqueue(new PostHubOrder(buyer, loc, ore, HubOrderSide.Buy, Quantity.From(10m), Money.From(3m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    await Assert.That(sim.State.Events.OfType<HubOrderFilled>().Count()).IsGreaterThan(0);
  }

  [Test]
  public async Task TreasuryAgent_OriginatesLoan_WhenBorrowerCashLow()
  {
    var treasury = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a5"));
    var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a6"));
    var builder = new EconomyWorldBuilder(new EconomyPolicy());
    builder.AddFirm(treasury, "Treasury", Money.From(20_000m));
    builder.AddFirm(borrower, "Mine", Money.From(500m));
    var sim = new EconomySimulation(13, builder.Build());

    var agent = new TreasuryFirmAgent(treasury, new TreasuryFirmAgentPolicy(
      [borrower], CashFloorToLend: 5_000m, BorrowerCashFloor: 2_000m,
      LoanPrincipal: Money.From(1_000m), AnnualInterestRate: 0.1m, TermHours: 240));
    agent.Tick(new AgentContext(sim, new DeterministicRandom(13)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));

    await Assert.That(sim.State.Events.OfType<LoanOriginated>().Count()).IsEqualTo(1);
    await Assert.That(sim.State.World.Ledgers[borrower].Cash.Amount).IsEqualTo(1_500m);
  }
}
