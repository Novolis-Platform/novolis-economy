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
using CoreLoanId = Novolis.Economy.Core.LoanId;
using CoreLoan = Novolis.Economy.Core.Loan;
using CoreLoanStatus = Novolis.Economy.Core.LoanStatus;
using CoreObligationId = Novolis.Economy.Core.ObligationId;
using CorePaymentObligation = Novolis.Economy.Core.PaymentObligation;
using CoreObligationKind = Novolis.Economy.Core.ObligationKind;
using CoreObligationStatus = Novolis.Economy.Core.ObligationStatus;

namespace Novolis.Economy.Unit;

static class OverTimeFixtures
{
    public static readonly RegionId RegionA = RegionId.From(Guid.Parse("f1000000-0000-0000-0000-000000000001"));
    public static readonly RegionId RegionB = RegionId.From(Guid.Parse("f1000000-0000-0000-0000-000000000002"));
    public static readonly LegalEntityId FirmId = LegalEntityId.From(Guid.Parse("f2000000-0000-0000-0000-000000000001"));
    public static readonly LegalEntityId BankId = LegalEntityId.From(Guid.Parse("f2000000-0000-0000-0000-000000000002"));
    public static readonly LegalEntityId HouseholdId = LegalEntityId.From(Guid.Parse("f2000000-0000-0000-0000-000000000003"));
    public static readonly LegalEntityId StateId = LegalEntityId.From(Guid.Parse("f2000000-0000-0000-0000-000000000004"));
    public static readonly LegalEntityId LenderId = LegalEntityId.From(Guid.Parse("f2000000-0000-0000-0000-000000000005"));
    public static readonly ResourceId OreId = ResourceId.From(Guid.Parse("f3000000-0000-0000-0000-000000000001"));
    public static readonly ResourceId WidgetId = ResourceId.From(Guid.Parse("f3000000-0000-0000-0000-000000000002"));
    public static readonly ActivityId ActId = ActivityId.From(Guid.Parse("f4000000-0000-0000-0000-000000000001"));

    public const decimal BankLoanCashOpen = 15m; // bank 10 + firm 5

    public static EconomyState ClosedFiscalNoTransfer()
    {
        var region = new Region(RegionA, 100, 100m, 100m);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 1,
            new HouseholdProfile(0m, 0.5m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(50m), HouseholdId);
        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(50m)),
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(40m)),
                [StateId] = new CoreEntity(StateId, CoreEntityKind.State, CoreMoney.From(30m)),
                [BankId] = new CoreEntity(BankId, CoreEntityKind.Bank, CoreMoney.From(20m))
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = StatePolicy.Neutral
        };
    }

    public static EconomyState SlowLane(int travelPeriods)
    {
        var a = new Region(RegionA, 100, 100m, 100m);
        var b = new Region(RegionB, 100, 100m, 100m);
        var lane = new TransportLane(RegionA, RegionB, travelPeriods, CapacityPerPeriod: 100m);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.Zero)
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
                [TransferEngine.LaneKey(RegionA, RegionB)] = lane
            },
            Policy = StatePolicy.Neutral
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, OreId, 10m);
    }

    public static EconomyState UnpayableWageDueAt(int duePeriod)
    {
        return EconomyState.Empty with
        {
            Period = 0,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(0m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(0m))
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, 10, 10m, 10m)
            },
            Obligations =
            [
                new CorePaymentObligation(
                    CoreObligationId.New(),
                    FirmId,
                    HouseholdId,
                    CoreMoney.From(50m),
                    DuePeriod: duePeriod,
                    CoreObligationKind.Wage,
                    CoreObligationStatus.Pending)
            ],
            Policy = StatePolicy.Neutral
        };
    }

    public static EconomyState SteadyProduction()
    {
        var recipe = new ActivityRecipe(
            [new ResourceAmount(OreId, 1m)],
            [new ResourceAmount(WidgetId, 1m)],
            LaborHoursPerRun: 1m,
            ProductionSpacePerRun: 1m);
        var activity = new Activity(ActId, FirmId, RegionA, recipe, InstalledCapacity: 5m);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 20,
            new HouseholdProfile(0m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(1m), HouseholdId);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(10m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(10m))
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, 100, 100m, 100m)
            },
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
            },
            Policy = StatePolicy.Neutral with { WagePerLaborHour = CoreMoney.From(0m) }
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, OreId, 100m);
    }

    public static EconomyState BankLoanWithInterest()
    {
        var loanId = CoreLoanId.From(Guid.Parse("f5000000-0000-0000-0000-000000000001"));
        return EconomyState.Empty with
        {
            Period = 0,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [BankId] = new CoreEntity(BankId, CoreEntityKind.Bank, CoreMoney.From(10m)),
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(5m))
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, 10, 10m, 10m)
            },
            Loans = new Dictionary<CoreLoanId, CoreLoan>
            {
                [loanId] = new CoreLoan(
                    loanId, BankId, FirmId, CoreMoney.From(100m), InterestRatePerPeriod: 0.05m,
                    RemainingPeriods: 20, CoreLoanStatus.Performing)
            },
            Deposits =
            [
                new Deposit(FirmId, BankId, CoreMoney.From(100m))
            ],
            Policy = StatePolicy.Neutral
        };
    }

    public static EconomyState FiscalTransfer(decimal stateCash, decimal transferPerHh)
    {
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 1,
            new HouseholdProfile(0m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(100m), HouseholdId);
        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(100m)),
                [StateId] = new CoreEntity(StateId, CoreEntityKind.State, CoreMoney.From(stateCash))
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, 100, 100m, 100m)
            },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = new StatePolicy(
                0m, 0m, CoreMoney.From(transferPerHh), 0m, 0m, CoreMoney.From(1m))
        };
    }

    public static EconomyState MinskyHorizon()
    {
        var loanId = CoreLoanId.From(Guid.Parse("f5000000-0000-0000-0000-000000000002"));
        return EconomyState.Empty with
        {
            Period = 0,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(50m)),
                [LenderId] = new CoreEntity(LenderId, CoreEntityKind.Lender, CoreMoney.From(100m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.Zero)
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, 10, 10m, 10m)
            },
            Loans = new Dictionary<CoreLoanId, CoreLoan>
            {
                [loanId] = new CoreLoan(
                    loanId, LenderId, FirmId, CoreMoney.From(30m), InterestRatePerPeriod: 0m, 20, CoreLoanStatus.Performing)
            },
            Obligations =
            [
                new CorePaymentObligation(
                    CoreObligationId.New(),
                    FirmId,
                    HouseholdId,
                    CoreMoney.From(100m),
                    DuePeriod: 1,
                    CoreObligationKind.Wage,
                    CoreObligationStatus.Pending)
            ],
            Policy = StatePolicy.Neutral
        };
    }
}
