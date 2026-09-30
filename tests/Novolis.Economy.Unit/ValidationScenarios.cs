using Novolis.Economy.Core;
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
using CoreObligationId = Novolis.Economy.Core.ObligationId;
using CorePaymentObligation = Novolis.Economy.Core.PaymentObligation;
using CoreLoanStatus = Novolis.Economy.Core.LoanStatus;
using CoreObligationKind = Novolis.Economy.Core.ObligationKind;
using CoreObligationStatus = Novolis.Economy.Core.ObligationStatus;

namespace Novolis.Economy.Unit;

file static class ValidationScenarios
{
    public static readonly RegionId RegionA = RegionId.From(Guid.Parse("a0000000-0000-0000-0000-000000000001"));
    public static readonly RegionId RegionB = RegionId.From(Guid.Parse("a0000000-0000-0000-0000-000000000002"));
    public static readonly LegalEntityId FirmId = LegalEntityId.From(Guid.Parse("b0000000-0000-0000-0000-000000000001"));
    public static readonly LegalEntityId BankId = LegalEntityId.From(Guid.Parse("b0000000-0000-0000-0000-000000000002"));
    public static readonly LegalEntityId HouseholdId = LegalEntityId.From(Guid.Parse("b0000000-0000-0000-0000-000000000003"));
    public static readonly LegalEntityId StateId = LegalEntityId.From(Guid.Parse("b0000000-0000-0000-0000-000000000004"));
    public static readonly LegalEntityId LenderId = LegalEntityId.From(Guid.Parse("b0000000-0000-0000-0000-000000000005"));
    public static readonly ResourceId WidgetId = ResourceId.From(Guid.Parse("c0000000-0000-0000-0000-000000000001"));
    public static readonly ResourceId OreId = ResourceId.From(Guid.Parse("c0000000-0000-0000-0000-000000000002"));

    public static decimal TotalCash(EconomyState state) =>
        state.Entities.Values.Sum(e => e.Cash.Amount);

    public static decimal TotalDeposits(EconomyState state) =>
        state.Deposits.Sum(d => d.Balance.Amount);

    public static decimal TotalResource(EconomyState state, ResourceId resourceId) =>
        state.Holdings.Values.Where(h => h.ResourceId.Equals(resourceId)).Sum(h => h.Quantity)
        + state.Transfers.Where(t => t.ResourceId.Equals(resourceId)).Sum(t => t.Quantity);

    public static EconomyState SimStyleOpen()
    {
        var region = new Region(RegionA, LivingCapacity: 100, ProductionCapacity: 100m, LogisticsCapacity: 100m);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 1,
            new HouseholdProfile(0m, 0.5m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(100m), HouseholdId);

        return EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(100m)),
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(50m)),
                [StateId] = new CoreEntity(StateId, CoreEntityKind.State, CoreMoney.From(100m)),
                [BankId] = new CoreEntity(BankId, CoreEntityKind.Bank, CoreMoney.From(20m))
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

    public static EconomyState CircuitOpen()
    {
        var region = new Region(RegionA, 100, 100m, 100m);
        var state = EconomyState.Empty with
        {
            Period = 0,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [BankId] = new CoreEntity(BankId, CoreEntityKind.Bank, CoreMoney.From(10m)),
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(5m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(0m))
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = region },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [WidgetId] = new Resource(
                    WidgetId,
                    "Widget",
                    ResourceKind.ConsumerGood,
                    EconomicAssetId.From(WidgetId.Value))
            }
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, WidgetId, 10m);
    }

    public static EconomyState MinskyFirm()
    {
        var loanId = CoreLoanId.From(Guid.Parse("d0000000-0000-0000-0000-000000000001"));
        return EconomyState.Empty with
        {
            Period = 1,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(50m)),
                [LenderId] = new CoreEntity(LenderId, CoreEntityKind.Lender, CoreMoney.From(100m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(0m))
            },
            Loans = new Dictionary<CoreLoanId, CoreLoan>
            {
                [loanId] = new CoreLoan(
                    loanId, LenderId, FirmId, CoreMoney.From(30m), 0.05m, 4, CoreLoanStatus.Performing)
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
            ]
        };
    }

    public static EconomyState RationingMarket()
    {
        var region = new Region(RegionA, 100, 100m, 100m);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 1,
            new HouseholdProfile(ConsumptionWeight: 1m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(1000m), HouseholdId);

        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(0m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(1000m))
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [WidgetId] = new Resource(
                    WidgetId,
                    "Widget",
                    ResourceKind.ConsumerGood,
                    EconomicAssetId.From(WidgetId.Value))
            },
            PostedPrices = new Dictionary<string, PostedPrice>
            {
                [EconomyState.PriceKey(RegionA, WidgetId)] =
                    new PostedPrice(RegionA, WidgetId, CoreMoney.From(10m))
            }
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, WidgetId, 3m);
    }

    public static EconomyState OvercrowdedRegion()
    {
        var region = new Region(RegionA, LivingCapacity: 2, ProductionCapacity: 100m, LogisticsCapacity: 100m);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, HouseholdCount: 5,
            new HouseholdProfile(0.5m, 0.2m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(1m));
        return EconomyState.Empty with
        {
            Regions = new Dictionary<RegionId, Region> { [RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort }
        };
    }

    public static EconomyState TightLogistics()
    {
        var a = new Region(RegionA, 100, 100m, LogisticsCapacity: 4m);
        var b = new Region(RegionB, 100, 100m, LogisticsCapacity: 100m);
        var lane = new TransportLane(RegionA, RegionB, TravelPeriods: 1, CapacityPerPeriod: 100m);
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
            }
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, OreId, 10m);
    }
}
