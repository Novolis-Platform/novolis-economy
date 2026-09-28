using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit.Simulation;

public sealed class CoreAuthorityProjectionTests
{
  [Test]
  public async Task LedgerCashChangeProjectsIntoCoreMonetaryPosition()
  {
    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-00000000c101"));
    var world = new EconomyWorldBuilder()
      .AddFirm(firm, "Seller", Money.From(100m))
      .Build();

    LedgerEngine.PostCashSale(
      world.Ledgers[firm],
      Money.From(25m),
      Money.Zero,
      SimulationDate.Epoch);

    await Assert.That(
        CashLedger.Balance(world.CoreState, firm.AsCore()))
      .IsEqualTo(Money.From(125m));
    await Assert.That(world.Ledgers[firm].Cash).IsEqualTo(Money.From(125m));
  }

  [Test]
  public async Task DirectInventorySeedProjectsBeforeSimulationRuns()
  {
    var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-00000000c102"));
    var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-00000000c103"));
    var location = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-00000000c104"));
    var world = new EconomyWorldBuilder()
      .AddFirm(firm, "Warehouse", Money.From(100m))
      .AddProduct(new ProductDefinition(
        product,
        ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-00000000c105")),
        [],
        [],
        ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-00000000c106")),
        null))
      .Build();

    world.Inventory.Add(
      new InventoryKey(firm, location, product),
      new ProductBatch(
        product,
        Quantity.From(7m),
        new ProductQuality(100m),
        Money.From(2m),
        SimulationDate.Epoch,
        null));

    _ = new EconomySimulation(42, world);

    await Assert.That(
        PositionLedger.GetQuantity(
          world.CoreState,
          EconomicIdentity.For(firm.AsCore()),
          region: null,
          world.CoreState.AssetFor(product.AsCore())))
      .IsEqualTo(7m);
  }
}
