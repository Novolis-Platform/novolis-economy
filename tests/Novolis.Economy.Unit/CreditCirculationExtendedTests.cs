using Novolis.Economy;
using Novolis.Economy.Finance;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Unit;

public sealed class CreditCirculationExtendedTests
{
    [Test]
    public async Task ObserveAfterPulse_AccumulatesAllFinanceAndLogisticsEvents()
    {
        var source = new FakeCreditSource();
        var tracker = new CreditCirculation(source);
        var hour = SimulationHour.Epoch;
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000001"));
        var other = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000002"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-000000000003"));
        var loanId = LoanId.From(Guid.Parse("00000000-0000-4000-8000-000000000004"));
        var facilityId = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-000000000005"));

        source.Events.AddRange(
        [
            new ProcurementFilled(hour, firm, product, Quantity.From(2m), Money.From(3m)),
            new TransportTollPaid(hour, Guid.NewGuid(), firm, Money.From(7m)),
            new BatchProduced(hour, firm, facilityId, product, Quantity.From(4m), Money.From(2m)),
            new GoodsSold(hour, firm, facilityId, ConsumerCohortId.From(Guid.NewGuid()), product, Quantity.From(1m), Money.From(9m), Money.From(9m)),
            new HubOrderFilled(hour, Guid.NewGuid(), Guid.NewGuid(), firm, other, InventoryLocationId.From(Guid.NewGuid()), product, Quantity.From(2m), Money.From(5m)),
            new ShipmentDeparted(hour, Guid.NewGuid(), firm, product, Quantity.From(10m)),
            new ShipmentPlanFailed(hour, firm, product, "no-feasible-path"),
            new InterestAccrued(hour, loanId, Money.From(1.5m)),
            new LoanRepaid(hour, loanId, Money.From(20m), Money.From(80m)),
            new LoanDefaulted(hour, loanId, firm, Money.From(50m)),
            new DividendPaid(hour, firm, other, Money.From(12m)),
            new OwnershipChanged(hour, firm, other, 0.5m),
            new CreditFrozenSet(hour, firm),
            new FacilityAbsorbed(hour, facilityId, firm, other),
            new FacilityUpgraded(hour, facilityId, firm, Money.From(25m), 1.2m, Quantity.From(12m)),
            new FacilityUpgradeFailed(hour, facilityId, "cash"),
        ]);

        tracker.SetFirmNames([(Name: "Alpha", Id: firm), (Name: "Beta", Id: other)]);
        tracker.ObserveAfterPulse(0);

        await Assert.That(tracker.ImportSpend).IsEqualTo(6m);
        await Assert.That(tracker.TollsToTreasury).IsEqualTo(7m);
        await Assert.That(tracker.Produced).IsEqualTo(4m);
        await Assert.That(tracker.RetailSold).IsEqualTo(1m);
        await Assert.That(tracker.BookFills).IsEqualTo(1);
        await Assert.That(tracker.Departed).IsEqualTo(1);
        await Assert.That(tracker.PlanFailReasons["no-feasible-path"]).IsEqualTo(1);
        await Assert.That(tracker.InterestAccrued).IsEqualTo(1.5m);
        await Assert.That(tracker.InterestPaid).IsEqualTo(20m);
        await Assert.That(tracker.LoansDefaulted).IsEqualTo(1);
        await Assert.That(tracker.Dividends).IsEqualTo(1);
        await Assert.That(tracker.OwnershipChanges).IsEqualTo(1);
        await Assert.That(tracker.CreditFreezes).IsEqualTo(1);
        await Assert.That(tracker.FacilitiesAbsorbed).IsEqualTo(1);
        await Assert.That(tracker.FacilityUpgrades).IsEqualTo(1);
        await Assert.That(tracker.FacilityUpgradeFails).IsEqualTo(1);
        await Assert.That(tracker.MacroLog.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task CaptureFinalMilestone_AndDay100Milestone()
    {
        var source = new FakeCreditSource { Clock = new SimulationHour(2400) };
        var tracker = new CreditCirculation(source);
        tracker.CaptureFinalMilestone();
        await Assert.That(tracker.Milestones.Count).IsEqualTo(1);

        source.Clock = new SimulationHour(2400);
        source.Events.Add(new BatchProduced(source.Clock, FirmId.From(Guid.NewGuid()), FacilityId.From(Guid.NewGuid()), ProductId.From(Guid.NewGuid()), Quantity.From(1m), Money.From(1m)));
        tracker.ObserveAfterPulse(0);
        await Assert.That(tracker.Milestones.Any(m => m.DayIndex == 100)).IsTrue();
    }

    [Test]
    public async Task MacroLog_TrimsToFortyLines()
    {
        var source = new FakeCreditSource();
        var tracker = new CreditCirculation(source);
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000aa"));
        for (var i = 0; i < 45; i++)
        {
            source.Events.Add(new CreditFrozenSet(new SimulationHour(i), firm));
        }

        tracker.ObserveAfterPulse(0);
        await Assert.That(tracker.MacroLog.Count).IsEqualTo(40);
    }

    private sealed class FakeCreditSource : ICreditCirculationSource
    {
        public List<IEconomyEvent> Events { get; } = [];
        public SimulationHour Clock { get; set; } = SimulationHour.Epoch;
        public decimal LiquidStock { get; set; } = 500m;
        public decimal HouseholdBudgets { get; set; } = 50m;
        public decimal FirmCash { get; set; } = 400m;
        public decimal InventoryBookValue { get; set; } = 120m;
        public decimal CargoDelivered { get; set; } = 15m;
        public int ActiveLoanCount { get; set; } = 2;
        public decimal PrincipalOutstanding { get; set; } = 300m;
        public int CreditFrozenFirmCount { get; set; }
        public int CorePeriod { get; set; } = 3;
        public decimal CoreTotalCash { get; set; } = 200m;
        public decimal CoreHoldingQty { get; set; } = 40m;
        public int CoreHoldingSlots { get; set; } = 5;
        public int CoreInFlightTransfers { get; set; } = 1;

        IReadOnlyList<IEconomyEvent> ICreditCirculationSource.Events => Events;

        public decimal InventoryQuantity(ProductId productId) => 0m;
    }
}
