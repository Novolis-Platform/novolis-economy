using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class EconomicAgentsExtendedTests
{
    [Test]
    public async Task RetailAgent_PostsBuyWhenStockLow()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a3"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b1"));
        var goods = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c3"));
        var fac = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e1"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddProduct(new ProductDefinition(
            goods, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(firm, "Shop", Money.From(5_000m));
        builder.AddFacility(new FacilityBinding(
            fac, firm, loc, loc,
            new FacilityLayout(
                ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty,
                ImmutableArray<MaterialRoute>.Empty)));
        var sim = new EconomySimulation(21, builder.Build());

        var agent = new RetailFirmAgent(firm, new RetailFirmAgentPolicy(
            [new AgentSite(loc, fac, Name: "store")],
            [],
            [new RetailSkuPolicy(goods, BaseRetailPrice: 12m, StockTarget: 20m, DeliveredLimitPrice: 8m, PostRetailPrice: true)],
            Bunker: null));
        agent.Tick(new AgentContext(sim, new DeterministicRandom(21)));

        await Assert.That(agent.LastDecision).Contains("bid");
    }

    [Test]
    public async Task ManufacturingAgent_SetsProductionPlanAndBuyInput()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a4"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000b2"));
        var input = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
        var output = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
        var fac = FacilityId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e2"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-000000000099"));
        var unit = OperatingUnitId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f1"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddProduct(new ProductDefinition(
            input, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddProduct(new ProductDefinition(
            output, cat, ImmutableArray.Create(new ProductInput(input, Quantity.From(1m))),
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(firm, "Plant", Money.From(8_000m));
        builder.AddFacility(new FacilityBinding(
            fac, firm, loc, loc,
            new FacilityLayout(
                ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty.Add(
                    unit, new OperatingUnit(unit, OperatingUnitKind.Manufacturing, Quantity.From(50m))),
                ImmutableArray<MaterialRoute>.Empty)));
        var sim = new EconomySimulation(22, builder.Build());

        var agent = new ManufacturingFirmAgent(firm, new ManufacturingFirmAgentPolicy(
            [new AgentSite(loc, fac, Name: "plant")],
            PrimaryInput: input,
            PrimaryInputFloor: 10m,
            PrimaryInputLimitPrice: 5m,
            Outputs:
            [
                new ManufacturedSkuPolicy(
                    output, BaseRate: 4m, StockTarget: 20m, MinInputOnHand: 2m,
                    RequiredInput: input, SellAboveStock: 15m, SellKeepFloor: 5m,
                    SellMaxQty: 10m, GatePrice: 9m),
            ]));
        agent.Tick(new AgentContext(sim, new DeterministicRandom(22)));

        await Assert.That(agent.LastDecision).IsNotEqualTo("manufacturing idle");
    }

    [Test]
    public async Task HouseholdAgent_OriginatesLoan_WhenAboveComfort()
    {
        var area = GeographicAreaId.From(Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001"));
        var hh = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000011"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000012"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy
        {
            HouseholdComfortThresholdPerHousehold = Money.From(50m),
            CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
        });
        builder.AddRegion(area, 10, 4);
        builder.AddFirm(borrower, "Borrower", Money.From(10m));
        builder.AddCohort(new ConsumerCohort(
            ConsumerCohortId.From(Guid.Parse("00000000-0000-4000-8000-0000000000a1")),
            new PopulationCount(1),
            Money.From(200m),
            new PreferenceProfile(ImmutableArray<CategoryPreference>.Empty, 1m, 1m, 0.5m),
            area,
            HouseholdFirmId: hh));
        var sim = new EconomySimulation(23, builder.Build());

        var agent = new HouseholdFirmAgent(hh, new HouseholdFirmAgentPolicy(
            PreferredBorrower: borrower,
            LoanPrincipal: Money.From(25m)));
        agent.Tick(new AgentContext(sim, new DeterministicRandom(23)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        await Assert.That(sim.State.Events.OfType<LoanOriginated>().Count()).IsEqualTo(1);
        await Assert.That(agent.LastDecision).Contains("lend");
    }
}
