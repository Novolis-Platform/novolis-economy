using Novolis.Economy;
using Novolis.Economy.Production;

namespace Novolis.Economy.Unit;

public class InventoryStoreLimitsTests
{
  [Test]
  public async Task HardCap_TruncatesAdd_AcrossFirmsAtLocation()
  {
    var store = new InventoryStore();
    var loc = InventoryLocationId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001"));
    var ore = ProductId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002"));
    var a = FirmId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0003"));
    var b = FirmId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0004"));
    store.Limits.Set(loc, ore, softCap: 5m, hardCap: 10m);

    var epoch = SimulationDate.Epoch;
    var accepted1 = store.Add(
      new InventoryKey(a, loc, ore),
      new ProductBatch(ore, Quantity.From(8m), new ProductQuality(100m), Money.From(1m), epoch, null));
    await Assert.That(accepted1.Value).IsEqualTo(8m);

    var accepted2 = store.Add(
      new InventoryKey(b, loc, ore),
      new ProductBatch(ore, Quantity.From(5m), new ProductQuality(100m), Money.From(1m), epoch, null));
    await Assert.That(accepted2.Value).IsEqualTo(2m);
    await Assert.That(InventoryStoreLimits.OnHand(store, loc, ore)).IsEqualTo(10m);
    await Assert.That(store.Limits.IsLargeSurplus(store, loc, ore)).IsTrue();
    await Assert.That(store.Limits.Surplus(store, loc, ore)).IsEqualTo(5m);
  }

  [Test]
  public async Task BypassLimits_IgnoresHardCap()
  {
    var store = new InventoryStore();
    var loc = InventoryLocationId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0011"));
    var ore = ProductId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0012"));
    var firm = FirmId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0013"));
    store.Limits.Set(loc, ore, softCap: 1m, hardCap: 2m);
    var epoch = SimulationDate.Epoch;
    store.Add(
      new InventoryKey(firm, loc, ore),
      new ProductBatch(ore, Quantity.From(9m), new ProductQuality(100m), Money.From(1m), epoch, null),
      bypassLimits: true);
    await Assert.That(InventoryStoreLimits.OnHand(store, loc, ore)).IsEqualTo(9m);
  }
}
