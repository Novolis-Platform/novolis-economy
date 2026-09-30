using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Simulation.Bounded;
using Novolis.Economy.Core.Transport;
using CoreMoney = Novolis.Economy.Primitives.Money;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;

namespace Novolis.Economy.Unit;

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
                [OreId] = new Resource(
                    OreId,
                    "Ore",
                    ResourceKind.IntermediateGood,
                    EconomicAssetId.From(OreId.Value)),
                [WidgetId] = new Resource(
                    WidgetId,
                    "Widget",
                    ResourceKind.ConsumerGood,
                    EconomicAssetId.From(WidgetId.Value))
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
                [OreId] = new Resource(
                    OreId,
                    "Ore",
                    ResourceKind.IntermediateGood,
                    EconomicAssetId.From(OreId.Value))
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
