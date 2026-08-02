using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Steps;
using CoreMoney = Novolis.Economy.Core.Money;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;

namespace Novolis.Economy.Unit;

public sealed class PeriodStepsBehaviorTests
{
    private static readonly RegionId Region = RegionId.From(Guid.Parse("a1000000-0000-0000-0000-000000000001"));
    private static readonly LegalEntityId Firm = LegalEntityId.From(Guid.Parse("b1000000-0000-0000-0000-000000000001"));
    private static readonly LegalEntityId Household = LegalEntityId.From(Guid.Parse("b1000000-0000-0000-0000-000000000002"));
    private static readonly LegalEntityId State = LegalEntityId.From(Guid.Parse("b1000000-0000-0000-0000-000000000003"));
    private static readonly ResourceId Widget = ResourceId.From(Guid.Parse("c1000000-0000-0000-0000-000000000001"));

    [Test]
    public async Task ApplyPolicyStep_TransfersPerHousehold_FromState()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(),
            Region,
            2,
            new HouseholdProfile(0m, 0m, 1m, 0m),
            HouseholdLaborKind.Common,
            CoreMoney.Zero,
            Household);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [State] = new CoreEntity(State, CoreEntityKind.State, CoreMoney.From(100m)),
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region> { [Region] = new Region(Region, 10, 10m, 10m) },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = StatePolicy.Neutral with { TransferPerHousehold = CoreMoney.From(5m) },
        };

        var next = new ApplyPolicyStep().Execute(state);
        await Assert.That(next.Period).IsEqualTo(1);
        await Assert.That(next.Entities[Household].Cash.Amount).IsEqualTo(10m);
        await Assert.That(next.Entities[State].Cash.Amount).IsEqualTo(90m);
    }

    [Test]
    public async Task TransferOwnershipPaymentsStep_BuysConsumerGoodFromFirm()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(),
            Region,
            1,
            new HouseholdProfile(1m, 0m, 1m, 0m),
            HouseholdLaborKind.Common,
            CoreMoney.From(20m),
            Household);
        var state = EconomyState.Empty with
        {
            Period = 0,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.Zero),
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.From(20m)),
            },
            Regions = new Dictionary<RegionId, Region> { [Region] = new Region(Region, 10, 10m, 10m) },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Widget] = new Resource(Widget, "Widget", ResourceKind.ConsumerGood),
            },
            PostedPrices = new Dictionary<string, PostedPrice>
            {
                ["widget"] = new PostedPrice(Region, Widget, CoreMoney.From(2m)),
            },
            Policy = StatePolicy.Neutral,
        };
        state = HoldingLedger.Credit(state, Firm, Region, Widget, 5m);

        var next = new TransferOwnershipPaymentsStep().Execute(state);
        await Assert.That(HoldingLedger.GetQuantity(next, Household, Region, Widget)).IsGreaterThan(0m);
        await Assert.That(next.Entities[Firm].Cash.Amount).IsGreaterThan(0m);
    }

    [Test]
    public async Task DrawCreditStep_DrawsWhenLiquidityShort()
    {
        var borrower = LegalEntityId.From(Guid.Parse("d1000000-0000-0000-0000-000000000001"));
        var lender = LegalEntityId.From(Guid.Parse("d1000000-0000-0000-0000-000000000002"));
        var facilityId = CreditFacilityId.New();
        var state = EconomyState.Empty with
        {
            Period = 1,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [borrower] = new CoreEntity(borrower, CoreEntityKind.Firm, CoreMoney.Zero),
                [lender] = new CoreEntity(lender, CoreEntityKind.Lender, CoreMoney.From(100m)),
            },
            CreditFacilities = new Dictionary<CreditFacilityId, CreditFacility>
            {
                [facilityId] = new CreditFacility(
                    facilityId,
                    lender,
                    borrower,
                    CoreMoney.From(50m),
                    CoreMoney.Zero,
                    IsCommitted: true),
            },
            Obligations =
            [
                new PaymentObligation(
                    ObligationId.New(),
                    borrower,
                    lender,
                    CoreMoney.From(80m),
                    DuePeriod: 1,
                    ObligationKind.Interest,
                    ObligationStatus.Pending),
            ],
            Policy = StatePolicy.Neutral,
        };

        var next = new DrawCreditStep().Execute(state);
        await Assert.That(next.Loans.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task CreateObligationsStep_AccruesWagesAndTaxes()
    {
        var activityId = ActivityId.New();
        var recipe = new ActivityRecipe([], [], LaborHoursPerRun: 2m, ProductionSpacePerRun: 1m);
        var activity = new Activity(activityId, Firm, Region, recipe, InstalledCapacity: 1m);
        var cohort = new HouseholdCohort(
            CohortId.New(),
            Region,
            1,
            new HouseholdProfile(0m, 0m, 1m, 0m),
            HouseholdLaborKind.Common,
            CoreMoney.Zero,
            Household);
        var state = EconomyState.Empty with
        {
            Period = 0,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.From(100m)),
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.From(50m)),
                [State] = new CoreEntity(State, CoreEntityKind.State, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region> { [Region] = new Region(Region, 10, 10m, 10m) },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Activities = new Dictionary<ActivityId, Activity> { [activityId] = activity },
            Scratch = PeriodScratch.Empty with
            {
                ActualRuns = new Dictionary<ActivityId, decimal> { [activityId] = 1m },
            },
            Policy = StatePolicy.Neutral with
            {
                WagePerLaborHour = CoreMoney.From(10m),
                FirmTaxRate = 0.1m,
            },
        };

        var next = new CreateObligationsStep().Execute(state);
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.Wage)).IsTrue();
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.Tax)).IsTrue();
    }

    [Test]
    public async Task MarkDelinquencyStep_DefaultsAfterTwoPeriods()
    {
        var obId = ObligationId.New();
        var state = EconomyState.Empty with
        {
            Period = 3,
            Obligations =
            [
                new PaymentObligation(
                    obId,
                    Firm,
                    Household,
                    CoreMoney.From(10m),
                    DuePeriod: 1,
                    ObligationKind.Wage,
                    ObligationStatus.Delinquent),
            ],
            Policy = StatePolicy.Neutral,
        };

        var next = new MarkDelinquencyStep().Execute(state);
        await Assert.That(next.Obligations.Single().Status).IsEqualTo(ObligationStatus.Defaulted);
    }
}
