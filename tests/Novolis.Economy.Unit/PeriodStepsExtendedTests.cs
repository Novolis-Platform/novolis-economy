using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Steps;
using CoreMoney = Novolis.Economy.Core.Money;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;
using CoreLoan = Novolis.Economy.Core.Loan;
using CoreLoanId = Novolis.Economy.Core.LoanId;
using CoreLoanStatus = Novolis.Economy.Core.LoanStatus;

namespace Novolis.Economy.Unit;

public sealed class PeriodStepsExtendedTests
{
    private static readonly RegionId RegionA = RegionId.From(Guid.Parse("a1000000-0000-0000-0000-000000000001"));
    private static readonly RegionId RegionB = RegionId.From(Guid.Parse("a1000000-0000-0000-0000-000000000002"));
    private static readonly LegalEntityId Firm = LegalEntityId.From(Guid.Parse("b1000000-0000-0000-0000-000000000001"));
    private static readonly LegalEntityId Shareholder = LegalEntityId.From(Guid.Parse("b1000000-0000-0000-0000-000000000002"));
    private static readonly LegalEntityId Household = LegalEntityId.From(Guid.Parse("b1000000-0000-0000-0000-000000000003"));
    private static readonly ResourceId Food = ResourceId.From(Guid.Parse("c1000000-0000-0000-0000-000000000001"));
    private static readonly CoreLoanId LoanId = CoreLoanId.From(Guid.Parse("f5000000-0000-0000-0000-000000000001"));

    [Test]
    public async Task DistributeDividendsStep_PaysShareholdersAboveRetention()
    {
        var state = EconomyState.Empty with
        {
            Period = 1,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.From(100m)),
                [Shareholder] = new CoreEntity(Shareholder, CoreEntityKind.Household, CoreMoney.Zero),
            },
            ShareHoldings =
            [
                new ShareHolding(Shareholder, Firm, "common", 100m),
            ],
            ShareClasses = new Dictionary<string, ShareClass>
            {
                ["common"] = new ShareClass(Firm, "common", 100m, 1m, 0m),
            },
            Policy = StatePolicy.Neutral,
        };

        var next = new DistributeDividendsStep().Execute(state);
        await Assert.That(next.Entities[Shareholder].Cash.Amount).IsGreaterThan(0m);
        await Assert.That(next.Entities[Firm].Cash.Amount).IsLessThan(100m);
    }

    [Test]
    public async Task HouseholdConsumeMigrateStep_ConsumesConsumerGoods()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(),
            RegionA,
            1,
            new HouseholdProfile(ConsumptionWeight: 0.5m, 0m, 1m, 0m),
            HouseholdLaborKind.Common,
            CoreMoney.Zero,
            Household);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 10m, 10m) },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Food] = new Resource(Food, "Food", ResourceKind.ConsumerGood),
            },
            Policy = StatePolicy.Neutral,
        };
        state = HoldingLedger.Credit(state, Household, RegionA, Food, 10m);

        var next = new HouseholdConsumeMigrateStep().Execute(state);
        await Assert.That(HoldingLedger.GetQuantity(next, Household, RegionA, Food)).IsEqualTo(5m);
    }

    [Test]
    public async Task HouseholdConsumeMigrateStep_MigratesTowardSpareCapacity()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(),
            RegionA,
            3,
            new HouseholdProfile(0m, 0m, 1m, MigrationPreference: 1m),
            HouseholdLaborKind.Common,
            CoreMoney.Zero,
            Household);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, LivingCapacity: 1, 10m, 10m),
                [RegionB] = new Region(RegionB, LivingCapacity: 20, 10m, 10m),
            },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = StatePolicy.Neutral,
        };

        var next = new HouseholdConsumeMigrateStep().Execute(state);
        await Assert.That(next.Cohorts[cohort.Id].RegionId).IsEqualTo(RegionB);
        await Assert.That(next.Scratch.HouseholdsMigrated).IsEqualTo(3);
    }

    [Test]
    public async Task HouseholdConsumeMigrateStep_TaxPush_Moves_Without_Overflow()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(),
            RegionA,
            8,
            new HouseholdProfile(0m, 0m, 1m, MigrationPreference: 0.8m),
            HouseholdLaborKind.Common,
            CoreMoney.Zero,
            Household);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, LivingCapacity: 100, 10m, 10m),
                [RegionB] = new Region(RegionB, LivingCapacity: 100, 10m, 10m),
            },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = StatePolicy.Neutral with { HouseholdTaxRate = 0.35m },
        };

        var next = new HouseholdConsumeMigrateStep().Execute(state);
        var inB = next.Cohorts.Values.Where(c => c.RegionId.Equals(RegionB)).Sum(c => c.HouseholdCount);
        var inA = next.Cohorts.Values.Where(c => c.RegionId.Equals(RegionA)).Sum(c => c.HouseholdCount);
        await Assert.That(inB).IsGreaterThan(0);
        await Assert.That(inA).IsLessThan(8);
        await Assert.That(next.Scratch.HouseholdsMigrated).IsGreaterThan(0);
        await Assert.That(inA + inB).IsEqualTo(8);
    }

    [Test]
    public async Task HouseholdConsumeMigrateStep_Low_Tax_Does_Not_Push_Without_Overflow()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(),
            RegionA,
            8,
            new HouseholdProfile(0m, 0m, 1m, MigrationPreference: 0.8m),
            HouseholdLaborKind.Common,
            CoreMoney.Zero,
            Household);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, LivingCapacity: 100, 10m, 10m),
                [RegionB] = new Region(RegionB, LivingCapacity: 100, 10m, 10m),
            },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = StatePolicy.Neutral with { HouseholdTaxRate = 0.15m },
        };

        var next = new HouseholdConsumeMigrateStep().Execute(state);
        await Assert.That(next.Cohorts[cohort.Id].RegionId).IsEqualTo(RegionA);
        await Assert.That(next.Scratch.HouseholdsMigrated).IsEqualTo(0);
    }

    [Test]
    public async Task MarkDelinquencyStep_MarksLoanDelinquentAndRepaid()
    {
        var lender = LegalEntityId.From(Guid.Parse("d1000000-0000-0000-0000-000000000001"));
        var borrower = LegalEntityId.From(Guid.Parse("d1000000-0000-0000-0000-000000000002"));
        var delinquentOb = ObligationId.New();
        var state = EconomyState.Empty with
        {
            Period = 2,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [lender] = new CoreEntity(lender, CoreEntityKind.Lender, CoreMoney.From(100m)),
                [borrower] = new CoreEntity(borrower, CoreEntityKind.Firm, CoreMoney.Zero),
            },
            Loans = new Dictionary<CoreLoanId, CoreLoan>
            {
                [LoanId] = new CoreLoan(
                    LoanId, lender, borrower, CoreMoney.From(50m), 0.05m, 4, CoreLoanStatus.Performing),
            },
            Obligations =
            [
                new PaymentObligation(
                    delinquentOb, borrower, lender, CoreMoney.From(5m), 1,
                    ObligationKind.Interest, ObligationStatus.Delinquent),
            ],
            Policy = StatePolicy.Neutral,
        };

        var next = new MarkDelinquencyStep().Execute(state);
        await Assert.That(next.Loans[LoanId].Status).IsEqualTo(CoreLoanStatus.Delinquent);
    }

    [Test]
    public async Task MarkDelinquencyStep_MarksRepaid_WhenPrincipalZeroAndNoPending()
    {
        var lender = LegalEntityId.From(Guid.Parse("d1000000-0000-0000-0000-000000000003"));
        var borrower = LegalEntityId.From(Guid.Parse("d1000000-0000-0000-0000-000000000004"));
        var repaidLoanId = CoreLoanId.From(Guid.Parse("f5000000-0000-0000-0000-000000000002"));
        var state = EconomyState.Empty with
        {
            Period = 1,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [lender] = new CoreEntity(lender, CoreEntityKind.Lender, CoreMoney.From(100m)),
                [borrower] = new CoreEntity(borrower, CoreEntityKind.Firm, CoreMoney.Zero),
            },
            Loans = new Dictionary<CoreLoanId, CoreLoan>
            {
                [repaidLoanId] = new CoreLoan(
                    repaidLoanId, lender, borrower, CoreMoney.Zero, 0.05m, 0, CoreLoanStatus.Performing),
            },
            Obligations = [],
            Policy = StatePolicy.Neutral,
        };

        var next = new MarkDelinquencyStep().Execute(state);
        await Assert.That(next.Loans[repaidLoanId].Status).IsEqualTo(CoreLoanStatus.Repaid);
    }

    [Test]
    public async Task ReconcileStep_PassesValidState()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(),
            RegionA,
            1,
            new HouseholdProfile(0m, 0m, 1m, 0m),
            HouseholdLaborKind.Common,
            CoreMoney.Zero,
            Household);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.From(10m)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 10m, 10m) },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = StatePolicy.Neutral,
        };

        var next = new ReconcileStep().Execute(state);
        await Assert.That(next.Period).IsEqualTo(state.Period);
        InvariantChecker.AssertAll(next);
    }
}
