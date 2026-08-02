using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Novolis.Economy.Simulation.Phases;

namespace Novolis.Economy.Unit.Simulation;

public sealed class SettleInvoicesAndWagesPhaseTests
{
    private static readonly SettleInvoicesAndWagesPhase Phase = new();

    private static PreferenceProfile Prefs() =>
        new(
            ImmutableArray<CategoryPreference>.Empty,
            PriceSensitivity: 1m,
            QualitySensitivity: 1m,
            BrandLoyalty: 0m);

    private static FacilityLayout MfgLayout(decimal capacity = 10m)
    {
        var unitId = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f1"));
        return new FacilityLayout(
            ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
                unitId, new OperatingUnit(unitId, OperatingUnitKind.Manufacturing, Quantity.From(capacity))),
            ImmutableArray<MaterialRoute>.Empty);
    }

    private static async Task<SimulationState> RunAsync(EconomyWorld world)
    {
        var state = new SimulationState(11, world);
        var ctx = new SimulationContext(state, new DeterministicRandom(11));
        await Phase.ExecuteAsync(ctx, CancellationToken.None);
        return state;
    }

    [Test]
    public async Task SkipsWagesWhenCashInsufficient()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
        var world = new EconomyWorldBuilder()
            .AddFirm(firm, "Broke", Money.From(0m))
            .Build();
        world.AccruedWages[firm] = Money.From(50m);

        var state = await RunAsync(world);
        await Assert.That(state.Events.OfType<WagesPaid>().Count()).IsEqualTo(0);
        await Assert.That(state.World.AccruedWages[firm].Amount).IsEqualTo(50m);
    }

    [Test]
    public async Task SkipsInvoiceWhenBuyerOrSellerMissing()
    {
        var seller = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
        var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b2"));
        var ghost = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b3"));
        var world = new EconomyWorldBuilder()
            .AddFirm(seller, "Seller", Money.From(10m))
            .Build();
        world.Invoices.Add(new Invoice(
            Guid.Parse("00000000-0000-4000-8000-0000000000b4"),
            seller,
            buyer,
            Money.From(20m),
            SimulationHour.Epoch));
        world.Invoices.Add(new Invoice(
            Guid.Parse("00000000-0000-4000-8000-0000000000b5"),
            ghost,
            seller,
            Money.From(15m),
            SimulationHour.Epoch));

        var state = await RunAsync(world);
        await Assert.That(state.Events.OfType<InvoiceSettled>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task PartialInvoicePaymentWhenBuyerCashLimited()
    {
        var seller = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
        var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
        var world = new EconomyWorldBuilder()
            .AddFirm(seller, "Seller", Money.From(0m))
            .AddFirm(buyer, "Buyer", Money.From(25m))
            .Build();
        world.Invoices.Add(new Invoice(
            Guid.Parse("00000000-0000-4000-8000-0000000000c3"),
            seller,
            buyer,
            Money.From(40m),
            SimulationHour.Epoch));

        var state = await RunAsync(world);
        await Assert.That(state.Events.OfType<InvoiceSettled>().Count()).IsEqualTo(1);
        await Assert.That(state.World.Invoices.Single().Remaining.Amount).IsEqualTo(15m);
    }

    [Test]
    public async Task HouseholdCreditFromWages_MultiAreaAndPopulationWeighted()
    {
        var areaA = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001"));
        var areaB = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000002"));
        var areaEmpty = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000003"));
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
        var facA = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d2"));
        var facB = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d3"));
        var facEmpty = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d4"));
        var locA = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d5"));
        var locB = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d6"));
        var locEmpty = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d7"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy { HouseholdCreditFromWages = true });
        builder.AddRegion(areaA, 10, 4);
        builder.AddRegion(areaB, 10, 4);
        builder.AddRegion(areaEmpty, 10, 4);
        builder.AddFirm(firm, "Plant", Money.From(1_000m));
        builder.AddFacility(new FacilityBinding(facA, firm, locA, null, MfgLayout(20m), areaA));
        builder.AddFacility(new FacilityBinding(facB, firm, locB, null, MfgLayout(10m), areaB));
        builder.AddFacility(new FacilityBinding(facEmpty, firm, locEmpty, null, MfgLayout(5m), areaEmpty));
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d8")),
            new PopulationCount(100),
            Money.From(0m),
            Prefs(),
            areaA));
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d9")),
            new PopulationCount(50),
            Money.From(0m),
            Prefs(),
            areaB));
        var world = builder.Build();
        world.AccruedWages[firm] = Money.From(150m);

        var state = await RunAsync(world);
        await Assert.That(state.Events.OfType<HouseholdCreditsIssued>().Count()).IsEqualTo(1);

        var cohortA = world.Cohorts.Single(c => c.Definition.Area.Equals(areaA));
        var cohortB = world.Cohorts.Single(c => c.Definition.Area.Equals(areaB));
        await Assert.That(cohortA.BudgetRemaining.Amount).IsGreaterThan(0m);
        await Assert.That(cohortB.BudgetRemaining.Amount).IsGreaterThan(0m);
        await Assert.That(cohortA.BudgetRemaining.Amount + cohortB.BudgetRemaining.Amount).IsEqualTo(150m);
    }

    [Test]
    public async Task DistributeWageCredits_GlobalFallbackWhenNoAreaCohorts()
    {
        var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000010"));
        var other = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000011"));
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e1"));
        var fac = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e2"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e3"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy { HouseholdCreditFromWages = true });
        builder.AddRegion(area, 10, 4);
        builder.AddFirm(firm, "Remote", Money.From(100m));
        builder.AddFacility(new FacilityBinding(fac, firm, loc, null, MfgLayout(), area));
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e4")),
            new PopulationCount(2),
            Money.From(0m),
            Prefs(),
            other));
        var world = builder.Build();

        SettleInvoicesAndWagesPhase.DistributeWageCreditsForFirm(world, firm, 80m);
        await Assert.That(world.Cohorts.Single().BudgetRemaining.Amount).IsEqualTo(80m);
    }

    [Test]
    public async Task DistributeWageCredits_NoOpForZeroAmount()
    {
        var world = new EconomyWorldBuilder().Build();
        SettleInvoicesAndWagesPhase.DistributeWageCreditsForFirm(world, FirmId.From(Guid.NewGuid()), 0m);
        await Assert.That(world.Cohorts.Count).IsEqualTo(0);
    }
}
