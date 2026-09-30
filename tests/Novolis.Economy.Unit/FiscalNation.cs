using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Simulation.Bounded;
using Novolis.Economy.Core.Transport;
using CoreMoney = Novolis.Economy.Primitives.Money;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;
using CoreObligationKind = Novolis.Economy.Core.ObligationKind;
using CoreObligationStatus = Novolis.Economy.Core.ObligationStatus;

namespace Novolis.Economy.Unit;

file static class FiscalNation
{
    public static readonly RegionId RegionA = RegionId.From(Guid.Parse("aa100000-0000-0000-0000-000000000001"));
    public static readonly RegionId RegionB = RegionId.From(Guid.Parse("aa100000-0000-0000-0000-000000000002"));
    public static readonly LegalEntityId FirmId = LegalEntityId.From(Guid.Parse("aa200000-0000-0000-0000-000000000001"));
    public static readonly LegalEntityId BankId = LegalEntityId.From(Guid.Parse("aa200000-0000-0000-0000-000000000002"));
    public static readonly LegalEntityId HouseholdId = LegalEntityId.From(Guid.Parse("aa200000-0000-0000-0000-000000000003"));
    public static readonly LegalEntityId StateId = LegalEntityId.From(Guid.Parse("aa200000-0000-0000-0000-000000000004"));
    public static readonly LegalEntityId InsurerId = LegalEntityId.From(Guid.Parse("aa200000-0000-0000-0000-000000000005"));
    public static readonly ResourceId WidgetId = ResourceId.From(Guid.Parse("aa300000-0000-0000-0000-000000000001"));
    public static readonly ResourceId OreId = ResourceId.From(Guid.Parse("aa300000-0000-0000-0000-000000000002"));
    public static readonly ActivityId ActId = ActivityId.From(Guid.Parse("aa400000-0000-0000-0000-000000000001"));

    public static EconomyState Open(
        decimal hhCash,
        decimal stateCash,
        decimal transfer,
        decimal hhTax,
        decimal firmCash = 0m,
        decimal firmTax = 0m)
    {
        var region = new Region(RegionA, 100, 100m, 100m);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 1,
            new HouseholdProfile(0m, 0.5m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(hhCash), HouseholdId);
        var entities = new Dictionary<LegalEntityId, CoreEntity>
        {
            [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(hhCash)),
            [StateId] = new CoreEntity(StateId, CoreEntityKind.State, CoreMoney.From(stateCash)),
        };
        if (firmCash > 0m || firmTax > 0m)
            entities[FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(firmCash));

        return EconomyState.Empty with
        {
            Entities = entities,
            Regions = new Dictionary<RegionId, Region> { [RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = new StatePolicy(
                HouseholdTaxRate: hhTax,
                FirmTaxRate: firmTax,
                TransferPerHousehold: CoreMoney.From(transfer),
                DepositReserveRequirement: 0m,
                InsuranceCapitalRequirement: 0m,
                WagePerLaborHour: CoreMoney.From(1m)),
        };
    }

    public static EconomyState Factory(decimal wage, decimal laborHours, decimal capacity, decimal ore)
    {
        var recipe = new ActivityRecipe(
            [new ResourceAmount(OreId, 1m)],
            [new ResourceAmount(WidgetId, 1m)],
            LaborHoursPerRun: laborHours,
            ProductionSpacePerRun: 1m);
        var activity = new Activity(ActId, FirmId, RegionA, recipe, InstalledCapacity: capacity);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 20,
            new HouseholdProfile(0m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(0m), HouseholdId);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(100m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(0m)),
                [StateId] = new CoreEntity(StateId, CoreEntityKind.State, CoreMoney.From(0m)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 100, 1000m, 100m) },
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
                    EconomicAssetId.From(WidgetId.Value)),
            },
            Policy = new StatePolicy(0m, 0m, CoreMoney.Zero, 0m, 0m, CoreMoney.From(wage)),
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, OreId, ore);
    }

    public static EconomyState BankCircuit()
    {
        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [BankId] = new CoreEntity(BankId, CoreEntityKind.Bank, CoreMoney.From(10m)),
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(5m)),
            },
        };
    }

    public static EconomyState BankLoanInterest(decimal principal, decimal rate)
    {
        var state = BankCircuit();
        state = CreditEngine.OriginateLoan(state, BankId, FirmId, CoreMoney.From(principal), rate, 4);
        return state;
    }

    public static EconomyState Market(decimal widgets, decimal hhCash, decimal price)
    {
        var region = new Region(RegionA, 100, 100m, 100m);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 1,
            new HouseholdProfile(ConsumptionWeight: 1m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(hhCash), HouseholdId);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(0m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(hhCash)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [WidgetId] = new Resource(
                    WidgetId,
                    "Widget",
                    ResourceKind.ConsumerGood,
                    EconomicAssetId.From(WidgetId.Value)),
            },
            PostedPrices = new Dictionary<string, PostedPrice>
            {
                [EconomyState.PriceKey(RegionA, WidgetId)] = new PostedPrice(RegionA, WidgetId, CoreMoney.From(price)),
            },
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, WidgetId, widgets);
    }

    public static EconomyState Lane(int travel, decimal qty, decimal logisticsCap = 100m)
    {
        var a = new Region(RegionA, 100, 100m, logisticsCap);
        var b = new Region(RegionB, 100, 100m, 100m);
        var lane = new TransportLane(RegionA, RegionB, travel, CapacityPerPeriod: 100m);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.Zero),
            },
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
                [TransferEngine.LaneKey(RegionA, RegionB)] = lane,
            },
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, OreId, qty);
    }

    public static EconomyState UnpayableWage()
    {
        return EconomyState.Empty with
        {
            Period = 1,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(5m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(0m)),
            },
            Obligations =
            [
                new PaymentObligation(
                    ObligationId.New(), FirmId, HouseholdId, CoreMoney.From(100m),
                    DuePeriod: 1, CoreObligationKind.Wage, CoreObligationStatus.Pending),
            ],
        };
    }

    public static EconomyState InsuredFirm(decimal premium)
    {
        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(100m)),
                [InsurerId] = new CoreEntity(InsurerId, CoreEntityKind.Insurer, CoreMoney.From(50m)),
            },
            Insurance =
            [
                new InsuranceCoverage(
                    InsurerId, FirmId, RiskKind.ProductionLoss,
                    CoveredFraction: 1m,
                    Deductible: CoreMoney.Zero,
                    PremiumPerPeriod: CoreMoney.From(premium)),
            ],
            Policy = StatePolicy.Neutral,
        };
    }

    public static EconomyState ThreeSector(decimal hh, decimal firm, decimal stateCash, decimal bank)
    {
        var region = new Region(RegionA, 100, 100m, 100m);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 1,
            new HouseholdProfile(0m, 0.5m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(hh), HouseholdId);
        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(hh)),
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(firm)),
                [StateId] = new CoreEntity(StateId, CoreEntityKind.State, CoreMoney.From(stateCash)),
                [BankId] = new CoreEntity(BankId, CoreEntityKind.Bank, CoreMoney.From(bank)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = new StatePolicy(0.03m, 0.02m, CoreMoney.From(2m), 0m, 0m, CoreMoney.From(1m)),
        };
    }
}
