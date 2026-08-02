using Novolis.Economy;
using Novolis.Economy.Finance;
using Novolis.Economy.Production;

namespace Novolis.Economy.Unit;

public sealed class CreditCirculationTests
{
    [Test]
    public async Task ObserveAfterPulse_AccumulatesWagesAndExports()
    {
        var source = new FakeCreditSource();
        var tracker = new CreditCirculation(source);
        var hour = SimulationHour.Epoch;
        source.Events.Add(new HouseholdCreditsIssued(hour, FirmId.From(Guid.NewGuid()), Money.From(100m)));
        source.Events.Add(new ExportFilled(
            hour,
            FirmId.From(Guid.NewGuid()),
            ProductId.From(Guid.NewGuid()),
            Quantity.From(5m),
            Money.From(2m),
            Money.From(10m)));

        tracker.ObserveAfterPulse(0);

        await Assert.That(tracker.WagesDistributed).IsEqualTo(100m);
        await Assert.That(tracker.ExportRevenue).IsEqualTo(10m);
        await Assert.That(tracker.ExportQty).IsEqualTo(5m);
        await Assert.That(tracker.ExportFills).IsEqualTo(1);
    }

    [Test]
    public async Task ObserveAfterPulse_RecordsMacroLogAndMilestone()
    {
        var source = new FakeCreditSource { Clock = new SimulationHour(24) };
        var tracker = new CreditCirculation(source);
        var lender = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000001"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c0"));
        source.Events.Add(new LoanOriginated(
            source.Clock,
            LoanId.From(Guid.NewGuid()),
            lender,
            borrower,
            Money.From(500m),
            0.1m,
            source.Clock.AddHours(240)));

        tracker.SetFirmNames([(Name: "Bank", Id: lender), (Name: "Tramp", Id: borrower)]);
        tracker.ObserveAfterPulse(0);

        await Assert.That(tracker.LoansOriginated).IsEqualTo(1);
        await Assert.That(tracker.TrampVentures).IsEqualTo(1);
        await Assert.That(tracker.MacroLog.Count).IsGreaterThan(0);
        await Assert.That(tracker.Milestones.Count).IsEqualTo(1);
    }

    [Test]
    public async Task InventoryBySku_UsesConfiguredProductIds()
    {
        var ore = ProductId.From(Guid.NewGuid());
        var parts = ProductId.From(Guid.NewGuid());
        var goods = ProductId.From(Guid.NewGuid());
        var fuel = ProductId.From(Guid.NewGuid());
        var source = new FakeCreditSource
        {
            InventoryQuantities =
            {
                [ore] = 10m,
                [parts] = 20m,
                [goods] = 30m,
                [fuel] = 40m,
            },
        };

        var tracker = new CreditCirculation(source);
        tracker.SetSkuIds(ore, parts, goods, fuel);
        var sku = tracker.InventoryBySku();

        await Assert.That(sku.Raw).IsEqualTo(10m);
        await Assert.That(sku.Capital).IsEqualTo(20m);
        await Assert.That(sku.Final).IsEqualTo(30m);
        await Assert.That(sku.Energy).IsEqualTo(40m);
    }

    [Test]
    public async Task ObserveAfterPulse_CountsB2bFailuresByReason()
    {
        var source = new FakeCreditSource();
        var tracker = new CreditCirculation(source);
        var seller = FirmId.From(Guid.NewGuid());
        var buyer = FirmId.From(Guid.NewGuid());
        source.Events.Add(new TransferGoodsFailed(source.Clock, seller, buyer, ProductId.From(Guid.NewGuid()), "cash"));
        source.Events.Add(new TransferGoodsFailed(source.Clock, seller, buyer, ProductId.From(Guid.NewGuid()), "stock"));

        tracker.ObserveAfterPulse(0);

        await Assert.That(tracker.B2bFailCash).IsEqualTo(1);
        await Assert.That(tracker.B2bFailStock).IsEqualTo(1);
    }

    private sealed class FakeCreditSource : ICreditCirculationSource
    {
        public List<IEconomyEvent> Events { get; } = [];
        public SimulationHour Clock { get; set; } = SimulationHour.Epoch;
        public decimal LiquidStock { get; set; } = 1_000m;
        public decimal HouseholdBudgets { get; set; }
        public decimal FirmCash { get; set; } = 800m;
        public decimal InventoryBookValue { get; set; }
        public decimal CargoDelivered { get; set; }
        public int ActiveLoanCount { get; set; }
        public decimal PrincipalOutstanding { get; set; }
        public int CreditFrozenFirmCount { get; set; }
        public Dictionary<ProductId, decimal> InventoryQuantities { get; } = [];
        public int CorePeriod { get; set; }
        public decimal CoreTotalCash { get; set; }
        public decimal CoreHoldingQty { get; set; }
        public int CoreHoldingSlots { get; set; }
        public int CoreInFlightTransfers { get; set; }

        IReadOnlyList<IEconomyEvent> ICreditCirculationSource.Events => Events;

        public decimal InventoryQuantity(ProductId productId) =>
            InventoryQuantities.GetValueOrDefault(productId);
    }
}
