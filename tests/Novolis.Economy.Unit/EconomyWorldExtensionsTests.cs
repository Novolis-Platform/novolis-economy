using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Core;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Novolis.Economy.Simulation.Extensions;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;
using CoreMoney = Novolis.Economy.Core.Money;

namespace Novolis.Economy.Unit;

public sealed class EconomyWorldExtensionsTests
{
    [Test]
    public async Task ToReportSnapshot_IncludesOpsSections()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
        var goods = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
        var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddProduct(new ProductDefinition(
            goods, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(firm, "Acme", Money.From(500m));
        builder.AddRegion(area, 50, 4);
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2")),
            new PopulationCount(10),
            Money.From(100m),
            new PreferenceProfile(ImmutableArray<CategoryPreference>.Empty, 1m, 1m, 0m),
            area));
        var world = builder.Build();
        world.Inventory.Add(
            new InventoryKey(firm, loc, goods),
            new ProductBatch(goods, Quantity.From(12m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));

        var snap = world.ToReportSnapshot();
        await Assert.That(snap.Ops.Ledgers.FirmCount).IsGreaterThanOrEqualTo(1);
        await Assert.That(snap.Ops.Inventory.SlotCount).IsEqualTo(1);
        await Assert.That(snap.Ops.Cohorts.Count).IsEqualTo(1);
        await Assert.That(snap.Core).IsNull();

        var text = WorldReportFormatter.Format(snap);
        await Assert.That(text).Contains("inventory slots 1");
        await Assert.That(text).Contains("cohorts 1");
    }

    [Test]
    public async Task ToReportSnapshot_IncludesCore_WhenCoreEntitiesPresent()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a3"));
        var coreFirm = LegalEntityId.From(Guid.Parse("b1000000-0000-0000-0000-000000000004"));
        var region = RegionId.From(Guid.Parse("a1000000-0000-0000-0000-000000000003"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddFirm(firm, "Bridge", Money.From(200m));
        var world = builder.Build();
        world.CoreState = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [coreFirm] = new CoreEntity(coreFirm, CoreEntityKind.Firm, CoreMoney.From(300m)),
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [region] = new Region(region, 10, 10m, 10m),
            },
            Policy = StatePolicy.Neutral,
        };

        var snap = world.ToReportSnapshot();
        await Assert.That(snap.Core).IsNotNull();
        await Assert.That(snap.Core!.Snapshot.TotalCash.Amount).IsEqualTo(300m);

        var text = WorldReportFormatter.Format(snap);
        await Assert.That(text).Contains("Core cash");
        await Assert.That(text).Contains("broad money");
        await Assert.That(text.Contains("combined", StringComparison.OrdinalIgnoreCase)).IsFalse();
    }
}
