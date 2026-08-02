using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Novolis.Economy.Simulation.Phases;

namespace Novolis.Economy.Unit.Simulation;

public sealed class EconomyPhasesIntegrationTests
{
    [Test]
    public async Task AcquireInputsPhase_FillsProcurementAndExport()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a2"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a3"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a4"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));

        var builder = new EconomyWorldBuilder();
        builder.AddProduct(new ProductDefinition(
            product, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(firm, "Trader", Money.From(100m));
        var world = builder.Build();
        world.Inventory.Add(
            new InventoryKey(firm, loc, product),
            new ProductBatch(product, Quantity.From(10m), new ProductQuality(100m), Money.From(2m), SimulationDate.Epoch, null));
        world.PendingProcurement.Add(new PlaceProcurementOrder(firm, loc, product, Quantity.From(5m), Money.From(4m)));
        world.PendingExports.Add(new PlaceExportOrder(firm, loc, product, Quantity.From(3m), Money.From(6m)));

        var state = new SimulationState(1, world);
        var ctx = new SimulationContext(state, new DeterministicRandom(1));
        await new AcquireInputsPhase().ExecuteAsync(ctx, CancellationToken.None);

        await Assert.That(state.Events.OfType<ProcurementFilled>().Count()).IsEqualTo(1);
        await Assert.That(state.Events.OfType<ExportFilled>().Count()).IsEqualTo(1);
        await Assert.That(state.World.PendingProcurement.Count).IsEqualTo(0);
        await Assert.That(state.World.PendingExports.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RunProductionPhase_ProducesOutput()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b2"));
        var input = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b3"));
        var output = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b4"));
        var fac = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b5"));
        var unit = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b6"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b7"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy { LaborHoursPerOutputUnit = 1m });
        builder.AddProduct(new ProductDefinition(
            input, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddProduct(new ProductDefinition(
            output, cat, ImmutableArray.Create(new ProductInput(input, Quantity.From(1m))),
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(firm, "Plant", Money.From(500m));
        builder.AddFacility(new FacilityBinding(
            fac, firm, loc, loc,
            new FacilityLayout(
                ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
                    unit, new OperatingUnit(unit, OperatingUnitKind.Manufacturing, Quantity.From(20m))),
                ImmutableArray<MaterialRoute>.Empty)));
        var world = builder.Build();
        world.Inventory.Add(
            new InventoryKey(firm, loc, input),
            new ProductBatch(input, Quantity.From(20m), new ProductQuality(100m), Money.From(1m), SimulationDate.Epoch, null));
        world.ProductionPlans[(firm, fac, output)] = Quantity.From(4m);
        world.AllocatedLaborHours[firm] = 10m;

        var state = new SimulationState(1, world);
        var ctx = new SimulationContext(state, new DeterministicRandom(1));
        await new RunProductionPhase().ExecuteAsync(ctx, CancellationToken.None);

        await Assert.That(state.Events.OfType<BatchProduced>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task AllocateLaborPhase_AccruesWagesForPlannedProduction()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
        var fac = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c3"));
        var world = new EconomyWorldBuilder(new EconomyPolicy { WageRatePerHour = Money.From(10m), LaborHoursPerOutputUnit = 2m })
            .AddFirm(firm, "Plant", Money.From(1_000m))
            .Build();
        world.AvailableLaborHours[firm] = 8m;
        world.ProductionPlans[(firm, fac, product)] = Quantity.From(2m);

        var state = new SimulationState(1, world);
        var ctx = new SimulationContext(state, new DeterministicRandom(1));
        await new AllocateLaborPhase().ExecuteAsync(ctx, CancellationToken.None);

        await Assert.That(state.World.AllocatedLaborHours[firm]).IsEqualTo(4m);
        await Assert.That(state.World.AccruedWages[firm].Amount).IsEqualTo(40m);
    }

    [Test]
    public async Task SettleInvoicesAndWagesPhase_PaysWagesAndInvoices()
    {
        var seller = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
        var buyer = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d2"));
        var world = new EconomyWorldBuilder(new EconomyPolicy { HouseholdCreditFromWages = false })
            .AddFirm(seller, "Seller", Money.From(50m))
            .AddFirm(buyer, "Buyer", Money.From(200m))
            .Build();
        world.AccruedWages[seller] = Money.From(30m);
        world.Invoices.Add(new Invoice(
            Guid.Parse("00000000-0000-4000-8000-0000000000e1"),
            seller,
            buyer,
            Money.From(40m),
            SimulationHour.Epoch));

        var state = new SimulationState(1, world);
        var ctx = new SimulationContext(state, new DeterministicRandom(1));
        await new SettleInvoicesAndWagesPhase().ExecuteAsync(ctx, CancellationToken.None);

        await Assert.That(state.Events.OfType<WagesPaid>().Count()).IsEqualTo(1);
        await Assert.That(state.Events.OfType<InvoiceSettled>().Count()).IsEqualTo(1);
        await Assert.That(state.World.Invoices.Single().IsSettled).IsTrue();
    }

    [Test]
    public async Task PhasePipeline_ThroughputMode_SkipsNonEssentialPhases()
    {
        var pipeline = PhasePipeline.CreateDefault();
        var state = new SimulationState(7, new EconomyWorld());
        var ctx = new SimulationContext(state, new DeterministicRandom(7)) { ThroughputMode = true };
        var executed = await pipeline.ExecuteAsync(ctx, CancellationToken.None);
        await Assert.That(executed).Contains(SimulationPhaseOrder.ApplyDecisions);
        await Assert.That(executed).Contains(SimulationPhaseOrder.MatchHubOrders);
        await Assert.That(executed).Contains(SimulationPhaseOrder.TransportInventory);
        await Assert.That(executed.Contains(SimulationPhaseOrder.RunProduction)).IsFalse();
    }
}
