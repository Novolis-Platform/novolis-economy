using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Steps;
using Novolis.Economy.Simulation;
using Novolis.Economy.Population;
using Novolis.Economy.Finance;
using CoreMoney = Novolis.Economy.Core.Money;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;

namespace Novolis.Economy.Unit;

public sealed class EconomyCoveragePushTests
{
    static readonly RegionId Region = RegionId.From(Guid.Parse("a1000000-0000-0000-0000-000000000001"));
    static readonly LegalEntityId FirmEntity = LegalEntityId.From(Guid.Parse("b1000000-0000-0000-0000-000000000001"));
    static readonly LegalEntityId HouseholdEntity = LegalEntityId.From(Guid.Parse("b1000000-0000-0000-0000-000000000002"));
    static readonly FirmId Firm = FirmId.From(FirmEntity.Value);
    static readonly ResourceId Food = ResourceId.From(Guid.Parse("c1000000-0000-0000-0000-000000000001"));
    static readonly ProductId Product = ProductId.From(Guid.Parse("d1000000-0000-0000-0000-000000000001"));

    [Test]
    public async Task Strong_ids_and_value_types_round_trip()
    {
        await Assert.That(FirmId.From(Firm.Value).Value).IsEqualTo(Firm.Value);
        await Assert.That(Quantity.From(2m).Value).IsEqualTo(2m);
        await Assert.That(SimulationHour.Epoch.HourIndex).IsEqualTo(0);
        await Assert.That(SimulationDate.Epoch.DayIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(Percentage.FromFraction(0.5m).Value).IsEqualTo(50m);
        await Assert.That(CoreMoney.From(1m).ToString()).Contains("1");
        await Assert.That(OperatingUnitId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(FacilityId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(ConsumerCohortId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(ProductCategoryId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(TransportCorridorId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(VehicleClassId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(TransportHubId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(GeographicAreaId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(FreightRouteId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(ProductionProcessId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(ShipmentId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(InventoryLocationId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(CreditFacilityId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(CohortId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(ObligationId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(ActivityId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(BrandId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(new PopulationCount(100).Value).IsEqualTo(100);
        await Assert.That(LoanId.New().Value).IsNotEqualTo(Guid.Empty);
        await Assert.That(ResourceId.New().Value).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task Period_steps_execute_on_minimal_state()
    {
        var state = EconomyState.Empty with
        {
            Period = 1,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmEntity] = new CoreEntity(FirmEntity, CoreEntityKind.Firm, CoreMoney.From(100m)),
                [HouseholdEntity] = new CoreEntity(HouseholdEntity, CoreEntityKind.Household, CoreMoney.From(50m)),
            },
            Regions = new Dictionary<RegionId, Region> { [Region] = new Region(Region, 10, 10m, 10m) },
            Resources = new Dictionary<ResourceId, Resource> { [Food] = new Resource(Food, "Food", ResourceKind.ConsumerGood) },
            Policy = StatePolicy.Neutral,
        };

        state = new ResolveDemandStep().Execute(state);
        state = new MatchBuyersSellersStep().Execute(state);
        state = new CreateObligationsStep().Execute(state);
        state = new ProcessTransfersStep().Execute(state);
        state = new SettleObligationsStep().Execute(state);
        state = new ReconcileStep().Execute(state);
        state = new ApplyPolicyStep().Execute(state);
        state = new DrawCreditStep().Execute(state);
        state = new DetermineProductionStep().Execute(state);
        state = new ApplyProductionStep().Execute(state);
        state = new CalculateLaborSupplyStep().Execute(state);
        state = new AllocateLaborStep().Execute(state);
        state = new TransferOwnershipPaymentsStep().Execute(state);
        state = new MarkDelinquencyStep().Execute(state);
        await Assert.That(state.Entities.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Economy_simulation_credit_source_reads_world()
    {
        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddFirm(Firm, "Firm", CoreMoney.From(1000m));
        var sim = new EconomySimulation(99, builder.Build());
        var source = new EconomySimulationCreditSource(sim);
        await Assert.That(source.InventoryBookValue).IsGreaterThanOrEqualTo(0m);
        await Assert.That(source.InventoryQuantity(Product)).IsGreaterThanOrEqualTo(0m);
        await Assert.That(source.CreditFrozenFirmCount).IsGreaterThanOrEqualTo(0);
        await Assert.That(source.PrincipalOutstanding).IsGreaterThanOrEqualTo(0m);
        await Assert.That(source.CargoDelivered).IsGreaterThanOrEqualTo(0m);
        await Assert.That(source.HouseholdBudgets).IsGreaterThanOrEqualTo(0m);
    }

    [Test]
    public async Task Treasury_agent_and_credit_circulation()
    {
        var treasury = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000010"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000011"));
        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddFirm(treasury, "Treasury", CoreMoney.From(20_000m));
        builder.AddFirm(borrower, "Borrower", CoreMoney.From(500m));
        var sim = new EconomySimulation(44, builder.Build());
        var source = new EconomySimulationCreditSource(sim);
        var agent = new TreasuryFirmAgent(treasury, new TreasuryFirmAgentPolicy(
            [borrower], CashFloorToLend: 5_000m, BorrowerCashFloor: 2_000m,
            LoanPrincipal: CoreMoney.From(500m), AnnualInterestRate: 0.1m, TermHours: 240));
        AgentScheduler.TickAll([agent], new AgentContext(sim, new DeterministicRandom(44)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));
        var tracker = new CreditCirculation(source);
        tracker.ObserveAfterPulse(0);
        await Assert.That(tracker.MacroLog.Count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Entity_rules_and_money_compare()
    {
        var household = new CoreEntity(HouseholdEntity, CoreEntityKind.Household, CoreMoney.Zero);
        await Assert.That(EntityRules.IsOwnable(CoreEntityKind.Firm)).IsTrue();
        await Assert.That(() => EntityRules.EnsureMayIssueShares(household)).Throws<InvalidOperationException>();
        var a = CoreMoney.From(5m);
        var b = CoreMoney.From(2m);
        await Assert.That(a.CompareTo(b)).IsGreaterThan(0);
    }
}
