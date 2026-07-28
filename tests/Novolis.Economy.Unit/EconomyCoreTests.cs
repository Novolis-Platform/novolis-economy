using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Steps;
using Novolis.Economy.Core.Transport;
using CoreMoney = Novolis.Economy.Core.Money;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;

namespace Novolis.Economy.Unit;

public sealed class EconomyCoreTests
{
    [Test]
    public async Task Empty_State_Starts_At_Period_Zero()
    {
        await Assert.That(EconomyState.Empty.Period).IsEqualTo(0);
        await Assert.That(EconomyState.Empty.Entities.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Default_Pipeline_Advances_Empty_Economy()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        await Assert.That(engine.Steps.Count).IsEqualTo(16);
        var next = engine.Advance(EconomyState.Empty);
        await Assert.That(next.Period).IsEqualTo(1);
        var again = engine.Advance(next);
        await Assert.That(again.Period).IsEqualTo(2);
    }

    [Test]
    public async Task HouseholdLabor_Hours_Match_Bands()
    {
        await Assert.That(HouseholdLabor.HoursPerDay(HouseholdLaborKind.Common)).IsEqualTo(12m);
        await Assert.That(HouseholdLabor.HoursPerDay(HouseholdLaborKind.Mean)).IsEqualTo(18m);
        await Assert.That(HouseholdLabor.HoursPerDay(HouseholdLaborKind.Extreme)).IsEqualTo(24m);
    }

    [Test]
    public async Task Production_Bottleneck_Is_Min_Of_Constraints()
    {
        var (state, activity) = CoreScenario.ProductionBottleneck();
        var runs = ProductionCalculator.ActualRuns(state, activity);
        await Assert.That(runs).IsEqualTo(2m);
        state = ProductionCalculator.ApplyRuns(state, activity, runs);
        var widgets = HoldingLedger.GetQuantity(state, activity.Operator, activity.RegionId, CoreScenario.WidgetId);
        await Assert.That(widgets).IsEqualTo(2m);
        var oreLeft = HoldingLedger.GetQuantity(state, activity.Operator, activity.RegionId, CoreScenario.OreId);
        await Assert.That(oreLeft).IsEqualTo(0m);
    }

    [Test]
    public async Task Transfer_Preserves_Ownership()
    {
        var state = CoreScenario.TwoRegionLane();
        var owner = CoreScenario.FirmId;
        state = TransferEngine.StartTransfer(state, owner, CoreScenario.OreId, 5m, CoreScenario.RegionA, CoreScenario.RegionB);
        await Assert.That(HoldingLedger.GetQuantity(state, owner, CoreScenario.RegionA, CoreScenario.OreId)).IsEqualTo(5m);
        await Assert.That(state.Transfers.Count).IsEqualTo(1);
        await Assert.That(state.Transfers[0].Owner).IsEqualTo(owner);

        state = TransferEngine.TickAndComplete(state);
        await Assert.That(state.Transfers.Count).IsEqualTo(0);
        await Assert.That(HoldingLedger.GetQuantity(state, owner, CoreScenario.RegionB, CoreScenario.OreId)).IsEqualTo(5m);
    }

    [Test]
    public async Task Bank_Loan_Creates_Deposit()
    {
        var state = CoreScenario.BankAndBorrower();
        state = CreditEngine.OriginateLoan(
            state, CoreScenario.BankId, CoreScenario.FirmId, CoreMoney.From(100m), 0.05m, 4);
        await Assert.That(state.Loans.Count).IsEqualTo(1);
        var dep = DepositLedger.TotalFor(state, CoreScenario.FirmId);
        await Assert.That(dep.Amount).IsEqualTo(100m);
        await Assert.That(state.Flows.MoneyCreated.Amount).IsEqualTo(100m);
        await Assert.That(state.Entities[CoreScenario.FirmId].Cash.Amount).IsEqualTo(10m);
    }

    [Test]
    public async Task Lender_Loan_Transfers_Cash()
    {
        var state = CoreScenario.LenderAndBorrower();
        var lenderCashBefore = state.Entities[CoreScenario.LenderId].Cash.Amount;
        state = CreditEngine.OriginateLoan(
            state, CoreScenario.LenderId, CoreScenario.FirmId, CoreMoney.From(40m), 0.05m, 2);
        await Assert.That(state.Entities[CoreScenario.FirmId].Cash.Amount).IsEqualTo(50m);
        await Assert.That(state.Entities[CoreScenario.LenderId].Cash.Amount).IsEqualTo(lenderCashBefore - 40m);
        await Assert.That(state.Deposits.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Obligation_Illiquidity_Marks_Delinquent()
    {
        var state = CoreScenario.BankAndBorrower();
        state = ObligationEngine.Create(
            state, CoreScenario.FirmId, CoreScenario.BankId, CoreMoney.From(1000m), duePeriod: 0, ObligationKind.Wage);
        state = ObligationEngine.SettleDue(state);
        var ob = state.Obligations.Single();
        await Assert.That(ob.Status).IsEqualTo(ObligationStatus.Delinquent);
    }

    [Test]
    public async Task Share_Consistency_Checked()
    {
        var state = CoreScenario.FirmWithShares();
        var sc = state.ShareClasses.Values.Single();
        await Assert.That(ShareMath.IsConsistent(state, sc)).IsTrue();
        InvariantChecker.AssertAll(state);

        state = ShareMath.UpsertHolding(state, CoreScenario.HouseholdId, CoreScenario.FirmId, "Common", 30m);
        var violations = InvariantChecker.Check(state);
        await Assert.That(violations.Any(v => v.Code == "SHARE_UNITS")).IsTrue();
    }

    [Test]
    public async Task Tax_Transfer_Conserves_Money()
    {
        var state = CoreScenario.Fiscal();
        var totalBefore = state.Entities.Values.Sum(e => e.Cash.Amount);
        var engine = DefaultPeriodPipeline.CreateEngine();
        var next = engine.Advance(state);
        var totalAfter = next.Entities.Values.Sum(e => e.Cash.Amount);
        await Assert.That(totalAfter).IsEqualTo(totalBefore);
    }

    [Test]
    public async Task Capacity_Clamps_Living_Invariant()
    {
        var region = new Region(CoreScenario.RegionA, LivingCapacity: 2, ProductionCapacity: 100m, LogisticsCapacity: 100m);
        var cohort = new HouseholdCohort(
            CohortId.New(), CoreScenario.RegionA, HouseholdCount: 5,
            new HouseholdProfile(0.5m, 0.2m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(1m));
        var state = EconomyState.Empty with
        {
            Regions = new Dictionary<RegionId, Region> { [CoreScenario.RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort }
        };
        var v = InvariantChecker.Check(state);
        await Assert.That(v.Any(x => x.Code == "CAP_LIVE")).IsTrue();
    }

    [Test]
    public async Task Household_Cannot_Issue_Shares()
    {
        await Assert.That(EntityRules.MayIssueShares(CoreEntityKind.Household)).IsFalse();
        await Assert.That(EntityRules.IsOwnable(CoreEntityKind.Household)).IsFalse();
    }
}

file static class CoreScenario
{
    public static readonly RegionId RegionA = RegionId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public static readonly RegionId RegionB = RegionId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    public static readonly LegalEntityId FirmId = LegalEntityId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    public static readonly LegalEntityId BankId = LegalEntityId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    public static readonly LegalEntityId LenderId = LegalEntityId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    public static readonly LegalEntityId HouseholdId = LegalEntityId.From(Guid.Parse("44444444-4444-4444-4444-444444444444"));
    public static readonly LegalEntityId StateId = LegalEntityId.From(Guid.Parse("55555555-5555-5555-5555-555555555555"));
    public static readonly ResourceId OreId = ResourceId.From(Guid.Parse("66666666-6666-6666-6666-666666666666"));
    public static readonly ResourceId WidgetId = ResourceId.From(Guid.Parse("77777777-7777-7777-7777-777777777777"));
    public static readonly ActivityId ActId = ActivityId.From(Guid.Parse("88888888-8888-8888-8888-888888888888"));

    public static (EconomyState State, Activity Activity) ProductionBottleneck()
    {
        var recipe = new ActivityRecipe(
            [new ResourceAmount(OreId, 2m)],
            [new ResourceAmount(WidgetId, 1m)],
            LaborHoursPerRun: 1m,
            ProductionSpacePerRun: 1m);
        var activity = new Activity(ActId, FirmId, RegionA, recipe, InstalledCapacity: 10m);
        var region = new Region(RegionA, 100, 100m, 100m);
        var firm = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(0m));
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 10,
            new HouseholdProfile(0.5m, 0.2m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(1m));

        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity> { [FirmId] = firm },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Activities = new Dictionary<ActivityId, Activity> { [ActId] = activity },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [OreId] = new Resource(OreId, "Ore", ResourceKind.IntermediateGood),
                [WidgetId] = new Resource(WidgetId, "Widget", ResourceKind.ConsumerGood)
            }
        };
        state = HoldingLedger.Credit(state, FirmId, RegionA, OreId, 4m);
        return (state, activity);
    }

    public static EconomyState TwoRegionLane()
    {
        var firm = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.Zero);
        var a = new Region(RegionA, 100, 100m, 100m);
        var b = new Region(RegionB, 100, 100m, 100m);
        var lane = new TransportLane(RegionA, RegionB, TravelPeriods: 1, CapacityPerPeriod: 100m);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity> { [FirmId] = firm },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = a, [RegionB] = b },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [OreId] = new Resource(OreId, "Ore", ResourceKind.IntermediateGood)
            },
            Lanes = new Dictionary<string, TransportLane>
            {
                [TransferEngine.LaneKey(RegionA, RegionB)] = lane
            }
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, OreId, 10m);
    }

    public static EconomyState BankAndBorrower()
    {
        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [BankId] = new CoreEntity(BankId, CoreEntityKind.Bank, CoreMoney.From(50m)),
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(10m))
            }
        };
    }

    public static EconomyState LenderAndBorrower()
    {
        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [LenderId] = new CoreEntity(LenderId, CoreEntityKind.Lender, CoreMoney.From(100m)),
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(10m))
            }
        };
    }

    public static EconomyState FirmWithShares()
    {
        var sc = new ShareClass(FirmId, "Common", IssuedUnits: 100m, VotesPerUnit: 1m, TreasuryUnits: 40m);
        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(100m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(10m))
            },
            ShareClasses = new Dictionary<string, ShareClass>
            {
                [ShareMath.ClassKey(FirmId, "Common")] = sc
            },
            ShareHoldings =
            [
                new ShareHolding(HouseholdId, FirmId, "Common", 60m)
            ]
        };
    }

    public static EconomyState Fiscal()
    {
        var region = new Region(RegionA, 100, 100m, 100m);
        var hh = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(100m));
        var st = new CoreEntity(StateId, CoreEntityKind.State, CoreMoney.From(100m));
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 1,
            new HouseholdProfile(0m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(100m), HouseholdId);
        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [HouseholdId] = hh,
                [StateId] = st
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = new StatePolicy(
                HouseholdTaxRate: 0m,
                FirmTaxRate: 0m,
                TransferPerHousehold: CoreMoney.From(10m),
                DepositReserveRequirement: 0m,
                InsuranceCapitalRequirement: 0m,
                WagePerLaborHour: CoreMoney.From(1m))
        };
    }
}
