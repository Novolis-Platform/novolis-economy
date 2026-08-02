using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Production;

namespace Novolis.Economy.Unit;

public sealed class ProductionEngineTests
{
    private static readonly FirmId Firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
    private static readonly InventoryLocationId Loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
    private static readonly ProductId Input = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
    private static readonly ProductId Output = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
    private static readonly ProductCategoryId Cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
    private static readonly ProductionProcessId Process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
    private static readonly SimulationDate ProducedAt = SimulationDate.Epoch;

    [Test]
    public async Task TryProduce_ConsumesInputs_AndAddsOutput()
    {
        var product = new ProductDefinition(
            Output, Cat,
            ImmutableArray.Create(new ProductInput(Input, Quantity.From(2m))),
            ImmutableArray<ProductAttributeDefinition>.Empty,
            Process, null);
        var inventory = new InventoryStore();
        inventory.Add(
            new InventoryKey(Firm, Loc, Input),
            new ProductBatch(Input, Quantity.From(10m), new ProductQuality(100m), Money.From(3m), ProducedAt, null));

        var made = ProductionEngine.TryProduce(
            product, inventory, Firm, Loc,
            plannedUnits: Quantity.From(3m),
            manufacturingCapacity: Quantity.From(10m),
            laborHours: 10m,
            laborHoursPerUnit: 1m,
            productivity: 1m,
            ProducedAt,
            out var unitCost);

        await Assert.That(made.Value).IsEqualTo(3m);
        await Assert.That(inventory.GetQuantity(new InventoryKey(Firm, Loc, Input)).Value).IsEqualTo(4m);
        await Assert.That(inventory.GetQuantity(new InventoryKey(Firm, Loc, Output)).Value).IsEqualTo(3m);
        await Assert.That(unitCost.Amount).IsEqualTo(6m);
    }

    [Test]
    public async Task TryProduce_ReturnsZero_WhenLaborOrInputsInsufficient()
    {
        var product = new ProductDefinition(
            Output, Cat,
            ImmutableArray.Create(new ProductInput(Input, Quantity.From(5m))),
            ImmutableArray<ProductAttributeDefinition>.Empty,
            Process, null);
        var inventory = new InventoryStore();
        inventory.Add(
            new InventoryKey(Firm, Loc, Input),
            new ProductBatch(Input, Quantity.From(2m), new ProductQuality(100m), Money.From(1m), ProducedAt, null));

        var made = ProductionEngine.TryProduce(
            product, inventory, Firm, Loc,
            plannedUnits: Quantity.From(5m),
            manufacturingCapacity: Quantity.From(10m),
            laborHours: 0m,
            laborHoursPerUnit: 1m,
            productivity: 1m,
            ProducedAt,
            out _);

        await Assert.That(made.Value).IsEqualTo(0m);
    }

    [Test]
    public async Task ApplySpoilage_RemovesExpiredLots_KeepsFresh()
    {
        var perishable = new ProductDefinition(
            Output, Cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty,
            Process, new ShelfLife(24));
        var products = new Dictionary<ProductId, ProductDefinition> { [Output] = perishable };
        var inventory = new InventoryStore();
        var oldKey = new InventoryKey(Firm, Loc, Output);
        var freshKey = new InventoryKey(Firm, Loc, Output);

        inventory.Add(oldKey, new ProductBatch(
            Output, Quantity.From(5m), new ProductQuality(100m), Money.From(2m),
            new SimulationDate(0), null));
        inventory.Add(freshKey, new ProductBatch(
            Output, Quantity.From(3m), new ProductQuality(100m), Money.From(2m),
            new SimulationDate(10), null));

        var now = new SimulationHour(48);
        var spoiled = ProductionEngine.ApplySpoilage(inventory, products, now);

        await Assert.That(spoiled.Count).IsEqualTo(1);
        await Assert.That(spoiled[0].Qty.Value).IsEqualTo(5m);
        await Assert.That(inventory.GetQuantity(oldKey).Value).IsEqualTo(3m);
    }
}
