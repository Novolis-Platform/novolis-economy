using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Steps;
using Novolis.Economy.Core.Transport;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;
using Novolis.Economy.Simulation.Phases;
using CoreMoney = Novolis.Economy.Core.Money;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;
using CoreLoan = Novolis.Economy.Core.Loan;
using CoreLoanId = Novolis.Economy.Core.LoanId;
using CoreLoanStatus = Novolis.Economy.Core.LoanStatus;

namespace Novolis.Economy.Unit;

/// <summary>Targeted coverage for Core public API / branch gaps toward 95%.</summary>
public sealed class EconomyCoreCoverageBoostTests
{
    static readonly RegionId RegionA = RegionId.From(Guid.Parse("a2000000-0000-0000-0000-000000000001"));
    static readonly RegionId RegionB = RegionId.From(Guid.Parse("a2000000-0000-0000-0000-000000000002"));
    static readonly LegalEntityId Firm = LegalEntityId.From(Guid.Parse("b2000000-0000-0000-0000-000000000001"));
    static readonly LegalEntityId Household = LegalEntityId.From(Guid.Parse("b2000000-0000-0000-0000-000000000002"));
    static readonly LegalEntityId State = LegalEntityId.From(Guid.Parse("b2000000-0000-0000-0000-000000000003"));
    static readonly LegalEntityId Bank = LegalEntityId.From(Guid.Parse("b2000000-0000-0000-0000-000000000004"));
    static readonly LegalEntityId Lender = LegalEntityId.From(Guid.Parse("b2000000-0000-0000-0000-000000000005"));
    static readonly LegalEntityId Insurer = LegalEntityId.From(Guid.Parse("b2000000-0000-0000-0000-000000000006"));
    static readonly ResourceId Food = ResourceId.From(Guid.Parse("c2000000-0000-0000-0000-000000000001"));
    static readonly ResourceId Ore = ResourceId.From(Guid.Parse("c2000000-0000-0000-0000-000000000002"));

    [Test]
    public async Task Money_Comparison_Operators_CoverAll()
    {
        var a = CoreMoney.From(3m);
        var b = CoreMoney.From(10m);
        var a2 = CoreMoney.From(3m);
        await Assert.That(a < b).IsTrue();
        await Assert.That(a <= b).IsTrue();
        await Assert.That(a <= a2).IsTrue();
        await Assert.That(b >= a).IsTrue();
        await Assert.That(a >= a2).IsTrue();
        await Assert.That(b > a).IsTrue();
    }

    [Test]
    public async Task Strong_Ids_ToString_And_EntityRules_MayInsure()
    {
        await Assert.That(CohortId.New().ToString().Length).IsEqualTo(32);
        await Assert.That(CreditFacilityId.New().ToString().Length).IsEqualTo(32);
        await Assert.That(ObligationId.New().ToString().Length).IsEqualTo(32);
        await Assert.That(ActivityId.New().ToString().Length).IsEqualTo(32);
        await Assert.That(LoanId.New().ToString().Length).IsEqualTo(32);
        await Assert.That(RegionId.New().ToString().Length).IsEqualTo(32);
        await Assert.That(EntityRules.MayInsure(CoreEntityKind.Insurer)).IsTrue();
        await Assert.That(EntityRules.MayInsure(CoreEntityKind.Firm)).IsFalse();
        var firm = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.Zero);
        EntityRules.EnsureMayIssueShares(firm);
        EntityRules.EnsureOwnableIssuer(firm);
    }

    [Test]
    public async Task Step_Names_Are_Stable()
    {
        await Assert.That(new ApplyPolicyStep().Name).IsEqualTo("01_ApplyPolicy");
        await Assert.That(new CalculateLaborSupplyStep().Name).IsEqualTo("02_CalculateLaborSupply");
        await Assert.That(new CreateObligationsStep().Name).IsEqualTo("10_CreateObligations");
        await Assert.That(new ApplyProductionStep().Name).IsEqualTo("05_ApplyProduction");
        await Assert.That(new DrawCreditStep().Name).IsEqualTo("12_DrawCredit");
        await Assert.That(new MarkDelinquencyStep().Name).IsEqualTo("13_MarkDelinquency");
        await Assert.That(new HouseholdConsumeMigrateStep().Name).IsEqualTo("15_HouseholdConsumeMigrate");
        await Assert.That(new ReconcileStep().Name).IsEqualTo("16_Reconcile");
    }

    [Test]
    public async Task ApplyPolicyStep_Credits_Cohort_Cash_Without_Entity_Link()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 2,
            new HouseholdProfile(0m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.Zero, HouseholdEntityId: null);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [State] = new CoreEntity(State, CoreEntityKind.State, CoreMoney.From(100m)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 10m, 10m) },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = StatePolicy.Neutral with { TransferPerHousehold = CoreMoney.From(5m) },
        };

        var next = new ApplyPolicyStep().Execute(state);
        await Assert.That(next.Cohorts[cohort.Id].CashPerHousehold.Amount).IsEqualTo(5m);
        await Assert.That(next.Entities[State].Cash.Amount).IsEqualTo(90m);
    }

    [Test]
    public async Task ApplyPolicyStep_Stops_When_State_Cash_Insufficient()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 10,
            new HouseholdProfile(0m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.Zero, HouseholdEntityId: null);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [State] = new CoreEntity(State, CoreEntityKind.State, CoreMoney.From(3m)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 20, 10m, 10m) },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Policy = StatePolicy.Neutral with { TransferPerHousehold = CoreMoney.From(5m) },
        };

        var next = new ApplyPolicyStep().Execute(state);
        await Assert.That(next.Entities[State].Cash.Amount).IsEqualTo(3m);
        await Assert.That(next.Cohorts[cohort.Id].CashPerHousehold.Amount).IsEqualTo(0m);
    }

    [Test]
    public async Task CreateObligationsStep_Principal_Insurance_And_Claims()
    {
        var loanId = CoreLoanId.New();
        var state = EconomyState.Empty with
        {
            Period = 2,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.From(80m)),
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.From(40m)),
                [State] = new CoreEntity(State, CoreEntityKind.State, CoreMoney.Zero),
                [Lender] = new CoreEntity(Lender, CoreEntityKind.Lender, CoreMoney.From(100m)),
                [Insurer] = new CoreEntity(Insurer, CoreEntityKind.Insurer, CoreMoney.From(200m)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 10m, 10m) },
            Loans = new Dictionary<CoreLoanId, CoreLoan>
            {
                [loanId] = new CoreLoan(
                    loanId, Lender, Firm, CoreMoney.From(50m), 0.05m,
                    RemainingPeriods: 0, CoreLoanStatus.Performing),
            },
            Insurance =
            [
                new InsuranceCoverage(
                    Insurer, Firm, RiskKind.ProductionLoss,
                    CoveredFraction: 0.8m, Deductible: CoreMoney.From(5m),
                    PremiumPerPeriod: CoreMoney.From(2m)),
            ],
            PendingLosses =
            [
                new LossEvent(Firm, RiskKind.ProductionLoss, CoreMoney.From(25m)),
            ],
            Policy = StatePolicy.Neutral with
            {
                HouseholdTaxRate = 0.1m,
                FirmTaxRate = 0.05m,
            },
        };

        var next = new CreateObligationsStep().Execute(state);
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.Principal)).IsTrue();
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.Interest)).IsTrue();
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.InsurancePremium)).IsTrue();
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.InsuranceClaim)).IsTrue();
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.Tax && o.Debtor.Equals(Household))).IsTrue();
        await Assert.That(next.PendingLosses).IsEmpty();
    }

    [Test]
    public async Task ObligationEngine_Priority_Covers_All_Kinds()
    {
        await Assert.That(ObligationEngine.Priority(ObligationKind.Wage)).IsEqualTo(0);
        await Assert.That(ObligationEngine.Priority(ObligationKind.Tax)).IsEqualTo(1);
        await Assert.That(ObligationEngine.Priority(ObligationKind.Interest)).IsEqualTo(2);
        await Assert.That(ObligationEngine.Priority(ObligationKind.Principal)).IsEqualTo(3);
        await Assert.That(ObligationEngine.Priority(ObligationKind.InsurancePremium)).IsEqualTo(4);
        await Assert.That(ObligationEngine.Priority(ObligationKind.InsuranceClaim)).IsEqualTo(5);
        await Assert.That(ObligationEngine.Priority(ObligationKind.Dividend)).IsEqualTo(6);
        await Assert.That(ObligationEngine.Priority(ObligationKind.Trade)).IsEqualTo(7);
        await Assert.That(ObligationEngine.Priority((ObligationKind)999)).IsEqualTo(99);
    }

    [Test]
    public async Task CreditEngine_Bank_Draw_And_Repay_Destroy_Deposits()
    {
        var facilityId = CreditFacilityId.New();
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Bank] = new CoreEntity(Bank, CoreEntityKind.Bank, CoreMoney.From(10m)),
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.Zero),
            },
            CreditFacilities = new Dictionary<CreditFacilityId, CreditFacility>
            {
                [facilityId] = new CreditFacility(
                    facilityId, Bank, Firm, CoreMoney.From(100m), CoreMoney.Zero, IsCommitted: true),
            },
            Policy = StatePolicy.Neutral,
        };

        await Assert.That(CreditEngine.DrawFacility(state, facilityId, CoreMoney.Zero, 0.01m, 4))
            .IsEqualTo(state);
        await Assert.That(() => CreditEngine.DrawFacility(state, CreditFacilityId.New(), CoreMoney.From(1m), 0.01m, 4))
            .Throws<InvalidOperationException>();
        await Assert.That(() => CreditEngine.DrawFacility(state, facilityId, CoreMoney.From(200m), 0.01m, 4))
            .Throws<InvalidOperationException>();

        var drawn = CreditEngine.DrawFacility(state, facilityId, CoreMoney.From(40m), 0.02m, 4);
        await Assert.That(drawn.Deposits.Sum(d => d.Balance.Amount)).IsEqualTo(40m);
        await Assert.That(drawn.Flows.MoneyCreated.Amount).IsEqualTo(40m);

        var loanId = drawn.Loans.Keys.Single();
        await Assert.That(CreditEngine.RepayPrincipal(drawn, loanId, CoreMoney.Zero)).IsEqualTo(drawn);
        await Assert.That(() => CreditEngine.RepayPrincipal(drawn, CoreLoanId.New(), CoreMoney.From(1m)))
            .Throws<InvalidOperationException>();

        var repaid = CreditEngine.RepayPrincipal(drawn, loanId, CoreMoney.From(15m));
        await Assert.That(repaid.Loans[loanId].PrincipalOutstanding.Amount).IsEqualTo(25m);
        await Assert.That(repaid.Flows.MoneyDestroyed.Amount).IsEqualTo(15m);

        var cleared = CreditEngine.RepayPrincipal(repaid, loanId, CoreMoney.From(100m));
        await Assert.That(cleared.Loans[loanId].Status).IsEqualTo(CoreLoanStatus.Repaid);
        await Assert.That(() => CreditEngine.RepayPrincipal(cleared, loanId, CoreMoney.From(1m)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task CreditEngine_Lender_Originate_And_Repay_Moves_Cash()
    {
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Lender] = new CoreEntity(Lender, CoreEntityKind.Lender, CoreMoney.From(80m)),
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.From(5m)),
            },
            Policy = StatePolicy.Neutral,
        };

        await Assert.That(CreditEngine.OriginateLoan(state, Lender, Firm, CoreMoney.Zero, 0.01m, 2))
            .IsEqualTo(state);
        var loaned = CreditEngine.OriginateLoan(state, Lender, Firm, CoreMoney.From(20m), 0.01m, 2);
        await Assert.That(loaned.Entities[Firm].Cash.Amount).IsEqualTo(25m);
        var loanId = loaned.Loans.Keys.Single();
        var repaid = CreditEngine.RepayPrincipal(loaned, loanId, CoreMoney.From(8m));
        await Assert.That(repaid.Entities[Lender].Cash.Amount).IsEqualTo(68m);
    }

    [Test]
    public async Task RegionCapacity_Logistics_And_MaxInstallableRuns()
    {
        var recipe = new ActivityRecipe([], [], 1m, ProductionSpacePerRun: 2m);
        var freeRecipe = new ActivityRecipe([], [], 1m, ProductionSpacePerRun: 0m);
        var state = EconomyState.Empty with
        {
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, 10, ProductionCapacity: 10m, LogisticsCapacity: 5m),
            },
            Activities = new Dictionary<ActivityId, Activity>
            {
                [ActivityId.New()] = new Activity(ActivityId.New(), Firm, RegionA, recipe, InstalledCapacity: 2m),
            },
            Transfers =
            [
                new ResourceTransfer(Firm, Food, 3m, RegionA, RegionB, RemainingPeriods: 1),
            ],
            Policy = StatePolicy.Neutral,
        };
        // Fix activity id consistency
        var actId = ActivityId.New();
        state = state with
        {
            Activities = new Dictionary<ActivityId, Activity>
            {
                [actId] = new Activity(actId, Firm, RegionA, recipe, InstalledCapacity: 2m),
            },
        };

        var region = state.Regions[RegionA];
        await Assert.That(RegionCapacity.LogisticsLoad(state, RegionA)).IsEqualTo(3m);
        await Assert.That(RegionCapacity.RemainingLogistics(state, region)).IsEqualTo(2m);
        await Assert.That(RegionCapacity.MaxInstallableRuns(state, region, freeRecipe)).IsEqualTo(decimal.MaxValue);
        await Assert.That(RegionCapacity.MaxInstallableRuns(state, region, recipe)).IsEqualTo(3m);

        var full = state with
        {
            Activities = new Dictionary<ActivityId, Activity>
            {
                [actId] = new Activity(actId, Firm, RegionA, recipe, InstalledCapacity: 10m),
            },
        };
        await Assert.That(RegionCapacity.MaxInstallableRuns(full, region, recipe)).IsEqualTo(0m);
    }

    [Test]
    public async Task TransferEngine_Start_And_Tick_Completes()
    {
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, 10, 10m, LogisticsCapacity: 100m),
                [RegionB] = new Region(RegionB, 10, 10m, 100m),
            },
            Lanes = new Dictionary<string, TransportLane>
            {
                [TransferEngine.LaneKey(RegionA, RegionB)] =
                    new TransportLane(RegionA, RegionB, TravelPeriods: 1, CapacityPerPeriod: 50m),
            },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Ore] = new Resource(Ore, "Ore", ResourceKind.IntermediateGood),
            },
            Policy = StatePolicy.Neutral,
        };
        state = HoldingLedger.Credit(state, Firm, RegionA, Ore, 10m);

        await Assert.That(TransferEngine.StartTransfer(state, Firm, Ore, 0m, RegionA, RegionB)).IsEqualTo(state);
        await Assert.That(() => TransferEngine.StartTransfer(state, Firm, Ore, 1m, RegionA, RegionA))
            .Throws<InvalidOperationException>();
        await Assert.That(() => TransferEngine.StartTransfer(state, Firm, Ore, 1m, RegionB, RegionA))
            .Throws<InvalidOperationException>();

        var flying = TransferEngine.StartTransfer(state, Firm, Ore, 4m, RegionA, RegionB);
        await Assert.That(flying.Transfers.Count).IsEqualTo(1);
        var arrived = TransferEngine.TickAndComplete(flying);
        await Assert.That(arrived.Transfers).IsEmpty();
        await Assert.That(HoldingLedger.GetQuantity(arrived, Firm, RegionB, Ore)).IsEqualTo(4m);
    }

    [Test]
    public async Task HoldingLedger_Debit_Guards_And_CashLedger_Insufficient()
    {
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.From(1m)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 10m, 10m) },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Food] = new Resource(Food, "Food", ResourceKind.ConsumerGood),
            },
            Policy = StatePolicy.Neutral,
        };
        state = HoldingLedger.Credit(state, Firm, RegionA, Food, 2m);

        await Assert.That(HoldingLedger.Debit(state, Firm, RegionA, Food, 0m)).IsEqualTo(state);
        await Assert.That(() => HoldingLedger.Debit(state, Firm, RegionA, Food, -1m))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => HoldingLedger.Debit(state, Firm, RegionA, Food, 5m))
            .Throws<InvalidOperationException>();
        await Assert.That(() => CashLedger.Debit(state, Firm, CoreMoney.From(5m)))
            .Throws<InvalidOperationException>();
        await Assert.That(() => CashLedger.SetCash(state, LegalEntityId.New(), CoreMoney.From(1m)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task InvariantChecker_Reports_Violations_And_AssertAll_Throws()
    {
        var badRegion = RegionId.New();
        var loanId = CoreLoanId.New();
        var facilityId = CreditFacilityId.New();
        var cohortId = CohortId.New();
        var missingParty = LegalEntityId.New();
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.From(-1m)),
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, LivingCapacity: 1, 1m, 1m) },
            Holdings = new Dictionary<string, ResourceHolding>
            {
                ["orphan"] = new ResourceHolding(LegalEntityId.New(), badRegion, ResourceId.New(), -2m),
            },
            ShareClasses = new Dictionary<string, ShareClass>
            {
                ["bad"] = new ShareClass(Household, "bad", 10m, 0m, 0m),
            },
            ShareHoldings =
            [
                new ShareHolding(Firm, Household, "bad", -1m),
            ],
            Loans = new Dictionary<CoreLoanId, CoreLoan>
            {
                [loanId] = new CoreLoan(
                    loanId, missingParty, missingParty,
                    CoreMoney.From(-3m), 0.01m, 1, CoreLoanStatus.Performing),
            },
            CreditFacilities = new Dictionary<CreditFacilityId, CreditFacility>
            {
                [facilityId] = new CreditFacility(
                    facilityId, Bank, Firm, CoreMoney.From(10m), CoreMoney.From(20m), true),
            },
            Cohorts = new Dictionary<CohortId, HouseholdCohort>
            {
                [cohortId] = new HouseholdCohort(
                    cohortId, badRegion, -2,
                    new HouseholdProfile(0m, 0m, 1m, 0m), HouseholdLaborKind.Common, CoreMoney.Zero, LegalEntityId.New()),
            },
            Deposits =
            [
                new Deposit(Firm, Firm, CoreMoney.From(-1m)),
            ],
            Transfers =
            [
                new ResourceTransfer(LegalEntityId.New(), Food, -1m, RegionA, RegionB, 1),
            ],
            Policy = StatePolicy.Neutral,
        };

        var violations = InvariantChecker.Check(state);
        await Assert.That(violations.Count).IsGreaterThan(5);
        await Assert.That(state.CheckInvariants().Count).IsEqualTo(violations.Count);
        await Assert.That(() => InvariantChecker.AssertAll(state)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task EconomyStateExtensions_Lists_And_Insights()
    {
        var facilityId = CreditFacilityId.New();
        var cohortId = CohortId.New();
        var loanId = CoreLoanId.New();
        var state = EconomyState.Empty with
        {
            Period = 2,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.From(5m)),
                [Lender] = new CoreEntity(Lender, CoreEntityKind.Lender, CoreMoney.From(50m)),
                [Bank] = new CoreEntity(Bank, CoreEntityKind.Bank, CoreMoney.From(10m)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 10m, 10m) },
            Cohorts = new Dictionary<CohortId, HouseholdCohort>
            {
                [cohortId] = new HouseholdCohort(
                    cohortId, RegionA, 4,
                    new HouseholdProfile(0.5m, 0m, 1m, 0m), HouseholdLaborKind.Common, CoreMoney.From(2m), Household),
            },
            CreditFacilities = new Dictionary<CreditFacilityId, CreditFacility>
            {
                [facilityId] = new CreditFacility(
                    facilityId, Lender, Firm, CoreMoney.From(30m), CoreMoney.Zero, IsCommitted: true),
            },
            Obligations =
            [
                new PaymentObligation(
                    ObligationId.New(), Firm, Lender, CoreMoney.From(40m), DuePeriod: 2,
                    ObligationKind.Wage, ObligationStatus.Pending),
            ],
            Loans = new Dictionary<CoreLoanId, CoreLoan>
            {
                [loanId] = new CoreLoan(
                    loanId, Lender, Firm, CoreMoney.From(100m), 0.01m, 2, CoreLoanStatus.Performing),
            },
            Policy = StatePolicy.Neutral,
        };

        await Assert.That(state.IlliquidEntities().Any(id => id.Equals(Firm))).IsTrue();
        await Assert.That(state.InsolventHintEntities().Any(id => id.Equals(Firm))).IsTrue();
        await Assert.That(state.DueObligations().Count).IsEqualTo(1);
        await Assert.That(state.UndrawnCommittedCredit().Amount).IsEqualTo(30m);
        await Assert.That(state.EntityInsights().Count).IsEqualTo(3);
        await Assert.That(state.RegionInsights().Count).IsEqualTo(1);
        await Assert.That(state.CohortInsights().Count).IsEqualTo(1);

        var entity = state.Entities[Firm];
        await Assert.That(entity.SimpleSolvency(state).Amount).IsLessThan(0m);
        await Assert.That(entity.ToInsight(state).IsIlliquid).IsTrue();
        await Assert.That(() => state.InsightFor(LegalEntityId.New())).Throws<InvalidOperationException>();
        await Assert.That(() => state.InsightFor(RegionId.New())).Throws<InvalidOperationException>();
        await Assert.That(() => state.ProjectedBooks(LegalEntityId.New())).Throws<InvalidOperationException>();

        var unpriced = state with
        {
            Holdings = new Dictionary<string, ResourceHolding>
            {
                [HoldingLedger.Key(Firm, RegionA, Ore)] = new ResourceHolding(Firm, RegionA, Ore, 7m),
            },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Ore] = new Resource(Ore, "Ore", ResourceKind.IntermediateGood),
            },
        };
        var books = unpriced.ProjectedBooks(Firm);
        await Assert.That(books.HoldingsUnpricedQuantity).IsEqualTo(7m);
    }

    [Test]
    public async Task ProductionCalculator_Zero_Capacity_And_ApplyRuns_Guards()
    {
        var recipe = new ActivityRecipe(
            [new ResourceAmount(Ore, 0m), new ResourceAmount(Ore, 1m)],
            [new ResourceAmount(Food, 0m), new ResourceAmount(Food, 1m)],
            LaborHoursPerRun: 0m, ProductionSpacePerRun: 0m);
        var activity = new Activity(ActivityId.New(), Firm, RegionA, recipe, InstalledCapacity: 0m);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 10m, 10m) },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Ore] = new Resource(Ore, "Ore", ResourceKind.IntermediateGood),
                [Food] = new Resource(Food, "Food", ResourceKind.ConsumerGood),
            },
            Policy = StatePolicy.Neutral,
        };
        state = HoldingLedger.Credit(state, Firm, RegionA, Ore, 5m);

        await Assert.That(ProductionCalculator.ActualRuns(state, activity)).IsEqualTo(0m);
        var live = activity with { InstalledCapacity = 3m };
        await Assert.That(ProductionCalculator.ActualRuns(state, live)).IsEqualTo(3m);
        await Assert.That(ProductionCalculator.ApplyRuns(state, live, 0m)).IsEqualTo(state);
        var next = ProductionCalculator.ApplyRuns(state, live, 2m);
        await Assert.That(HoldingLedger.GetQuantity(next, Firm, RegionA, Food)).IsEqualTo(2m);
    }

    [Test]
    public async Task DepositLedger_Guards_And_TryPayFromDeposits()
    {
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Bank] = new CoreEntity(Bank, CoreEntityKind.Bank, CoreMoney.From(5m)),
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.Zero),
                [Lender] = new CoreEntity(Lender, CoreEntityKind.Lender, CoreMoney.Zero),
            },
            Policy = StatePolicy.Neutral,
        };

        await Assert.That(DepositLedger.Credit(state, Firm, Bank, CoreMoney.Zero)).IsEqualTo(state);
        await Assert.That(() => DepositLedger.Credit(state, Firm, Lender, CoreMoney.From(1m)))
            .Throws<InvalidOperationException>();
        state = DepositLedger.Credit(state, Firm, Bank, CoreMoney.From(10m));
        await Assert.That(DepositLedger.Debit(state, Firm, Bank, CoreMoney.Zero)).IsEqualTo(state);
        await Assert.That(() => DepositLedger.Debit(state, Firm, Bank, CoreMoney.From(50m)))
            .Throws<InvalidOperationException>();

        var paid = false;
        var working = state;
        paid = DepositLedger.TryPayFromDeposits(ref working, Firm, Lender, CoreMoney.From(4m));
        await Assert.That(paid).IsTrue();
        await Assert.That(DepositLedger.TotalFor(working, Lender).Amount).IsEqualTo(4m);
        await Assert.That(DepositLedger.TryPayFromDeposits(ref working, Firm, Lender, CoreMoney.Zero)).IsTrue();
    }

    [Test]
    public async Task SimulationDate_Hour_And_PhaseExecuted_RoundTrip()
    {
        var day = SimulationDate.Epoch.AddDays(2);
        await Assert.That(day.DayIndex).IsEqualTo(2);
        await Assert.That(day.CompareTo(SimulationDate.Epoch)).IsGreaterThan(0);
        await Assert.That(day.ToString()).IsEqualTo("D2");

        var hour = SimulationHour.Epoch.AddHours(26);
        await Assert.That(hour.Date.DayIndex).IsEqualTo(1);
        await Assert.That(hour.HourOfDay).IsEqualTo(2);
        await Assert.That(hour.CompareTo(SimulationHour.Epoch)).IsGreaterThan(0);
        await Assert.That(hour.ToString()).IsEqualTo("H26");

        var ev = new PhaseExecuted(hour, SimulationPhaseOrder.ApplyResearchProgress);
        await Assert.That(ev.Phase).IsEqualTo(SimulationPhaseOrder.ApplyResearchProgress);
    }

    [Test]
    public async Task EconomySimulationCreditSource_Inventory_And_Core_Metrics()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c1"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c2"));
        var product = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c3"));
        var other = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c4"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c5"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-0000000000c6"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddProduct(new ProductDefinition(
            product, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddProduct(new ProductDefinition(
            other, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(firm, "Mine", CoreMoney.From(100m));
        builder.AddInventory(firm, loc, new ProductBatch(
            product, Quantity.From(5m), new ProductQuality(100m), CoreMoney.From(2m), SimulationDate.Epoch, null));
        builder.AddInventory(firm, loc, new ProductBatch(
            other, Quantity.From(3m), new ProductQuality(100m), CoreMoney.From(1m), SimulationDate.Epoch, null));
        var sim = new EconomySimulation(77, builder.Build());
        sim.State.World.ResearchBudget[firm] = CoreMoney.From(10m);

        var source = new EconomySimulationCreditSource(sim);
        await Assert.That(source.InventoryBookValue).IsEqualTo(13m);
        await Assert.That(source.InventoryQuantity(product)).IsEqualTo(5m);
        await Assert.That(source.InventoryQuantity(other)).IsEqualTo(3m);
        await Assert.That(source.CorePeriod).IsGreaterThanOrEqualTo(0);
        await Assert.That(source.CoreTotalCash).IsGreaterThanOrEqualTo(0m);
        await Assert.That(source.CoreHoldingSlots).IsGreaterThanOrEqualTo(0);
        await Assert.That(source.CoreInFlightTransfers).IsGreaterThanOrEqualTo(0);
        await Assert.That(source.HouseholdBudgets).IsGreaterThanOrEqualTo(0m);

        await new ApplyResearchProgressPhase().ExecuteAsync(
            new SimulationContext(sim.State, new DeterministicRandom(77)), CancellationToken.None);
        await Assert.That(sim.State.World.ResearchBudget[firm].Amount).IsEqualTo(0m);
        await Assert.That(sim.State.World.Productivity[firm]).IsGreaterThan(1m);
    }

    [Test]
    public async Task RetailAgent_Bunker_Policy_And_HubOrderQuotes_CancelFilters()
    {
        var firm = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d1"));
        var loc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d2"));
        var otherLoc = InventoryLocationId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d3"));
        var fuel = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d4"));
        var goods = ProductId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d5"));
        var cat = ProductCategoryId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d6"));
        var process = ProductionProcessId.From(Guid.Parse("00000000-0000-4000-8000-0000000000d7"));

        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddProduct(new ProductDefinition(
            fuel, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddProduct(new ProductDefinition(
            goods, cat, ImmutableArray<ProductInput>.Empty,
            ImmutableArray<ProductAttributeDefinition>.Empty, process, null));
        builder.AddFirm(firm, "Bunker", CoreMoney.From(5_000m));
        var sim = new EconomySimulation(88, builder.Build());

        var bunker = new BunkerSkuPolicy(fuel, MinStock: 10m, BuyLimitPrice: 2m, SellPrice: 3m, AllowProcurement: true);
        var agent = new RetailFirmAgent(firm, new RetailFirmAgentPolicy(
            RetailSites: [],
            BunkerSites: [new AgentSite(loc, FacilityId: null, Name: "dock")],
            RetailSkus: [],
            Bunker: bunker));
        agent.Tick(new AgentContext(sim, new DeterministicRandom(88)));
        await Assert.That(agent.LastDecision).Contains("import bunker");

        sim.State.World.Inventory.Add(
            new InventoryKey(firm, loc, fuel),
            new ProductBatch(fuel, Quantity.From(30m), new ProductQuality(100m), CoreMoney.From(1m), SimulationDate.Epoch, null));
        agent.Tick(new AgentContext(sim, new DeterministicRandom(88)));

        var keep = Guid.NewGuid();
        var cancelLoc = Guid.NewGuid();
        var cancelSide = Guid.NewGuid();
        sim.State.World.HubOrders.Add(new HubOrder(
            keep, firm, loc, fuel, HubOrderSide.Buy, Quantity.From(1m), CoreMoney.From(1m), SimulationHour.Epoch));
        sim.State.World.HubOrders.Add(new HubOrder(
            cancelLoc, firm, otherLoc, fuel, HubOrderSide.Buy, Quantity.From(1m), CoreMoney.From(1m), SimulationHour.Epoch));
        sim.State.World.HubOrders.Add(new HubOrder(
            cancelSide, firm, loc, goods, HubOrderSide.Sell, Quantity.From(1m), CoreMoney.From(1m), SimulationHour.Epoch));

        var before = sim.State.PendingCommands.Count;
        var ctx = new AgentContext(sim, new DeterministicRandom(88));
        HubOrderQuotes.CancelOpen(ctx, firm, location: loc, product: fuel, side: HubOrderSide.Buy);
        var cancels = sim.State.PendingCommands.Skip(before).OfType<CancelHubOrder>().ToList();
        await Assert.That(cancels.Any(c => c.OrderId == keep)).IsTrue();
        await Assert.That(cancels.Any(c => c.OrderId == cancelLoc)).IsFalse();
    }

    [Test]
    public async Task TreasuryFirmAgent_Thin_Idle_And_Skip_Paths()
    {
        var treasury = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e1"));
        var rich = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e2"));
        var frozen = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000e3"));
        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddFirm(treasury, "Treasury", CoreMoney.From(100m));
        builder.AddFirm(rich, "Rich", CoreMoney.From(10_000m));
        builder.AddFirm(frozen, "Frozen", CoreMoney.From(10m));
        var sim = new EconomySimulation(91, builder.Build());
        sim.State.World.Entities[frozen].CreditFrozen = true;

        var thin = new TreasuryFirmAgent(treasury, new TreasuryFirmAgentPolicy(
            [rich], CashFloorToLend: 5_000m, BorrowerCashFloor: 2_000m,
            LoanPrincipal: CoreMoney.From(500m), AnnualInterestRate: 0.1m, TermHours: 24));
        thin.Tick(new AgentContext(sim, new DeterministicRandom(91)));
        await Assert.That(thin.LastDecision).IsEqualTo("treasury thin");

        sim.State.World.Ledgers[treasury].SeedCash(CoreMoney.From(20_000m), SimulationDate.Epoch);
        var idle = new TreasuryFirmAgent(treasury, new TreasuryFirmAgentPolicy(
            [rich, frozen], CashFloorToLend: 5_000m, BorrowerCashFloor: 2_000m,
            LoanPrincipal: CoreMoney.From(500m), AnnualInterestRate: 0.1m, TermHours: 24));
        idle.Tick(new AgentContext(sim, new DeterministicRandom(91)));
        await Assert.That(idle.LastDecision).IsEqualTo("treasury idle");
    }

    [Test]
    public async Task RegionExtensions_Utilization_Helpers()
    {
        var state = EconomyState.Empty with
        {
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 20m, 10m) },
            Policy = StatePolicy.Neutral,
        };
        var region = state.Regions[RegionA];
        await Assert.That(region.LivingUtilization(state)).IsEqualTo(0m);
        await Assert.That(region.ProductionUtilization(state)).IsEqualTo(0m);
        await Assert.That(region.ToInsight(state).Id).IsEqualTo(RegionA);
    }

    [Test]
    public async Task CreateObligationsStep_Skips_Empty_Branches()
    {
        var activityId = ActivityId.New();
        var ghostActivity = ActivityId.New();
        var zeroLabor = new Activity(
            activityId, Firm, RegionA,
            new ActivityRecipe([], [], LaborHoursPerRun: 0m, ProductionSpacePerRun: 1m), 1m);
        var loanId = CoreLoanId.New();
        var state = EconomyState.Empty with
        {
            Period = 1,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.From(10m)),
                [Lender] = new CoreEntity(Lender, CoreEntityKind.Lender, CoreMoney.From(10m)),
                [Insurer] = new CoreEntity(Insurer, CoreEntityKind.Insurer, CoreMoney.From(10m)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 10m, 10m) },
            Activities = new Dictionary<ActivityId, Activity> { [activityId] = zeroLabor },
            Scratch = PeriodScratch.Empty with
            {
                ActualRuns = new Dictionary<ActivityId, decimal>
                {
                    [ghostActivity] = 1m,
                    [activityId] = 1m,
                },
            },
            Loans = new Dictionary<CoreLoanId, CoreLoan>
            {
                [loanId] = new CoreLoan(
                    loanId, Lender, Firm, CoreMoney.Zero, 0.05m, RemainingPeriods: 0, CoreLoanStatus.Performing),
            },
            Insurance =
            [
                new InsuranceCoverage(
                    Insurer, Firm, RiskKind.TransportLoss, 0.5m, CoreMoney.From(10m), CoreMoney.Zero),
            ],
            PendingLosses =
            [
                new LossEvent(Firm, RiskKind.TransportLoss, CoreMoney.From(5m)),
            ],
            Policy = StatePolicy.Neutral,
        };

        var next = new CreateObligationsStep().Execute(state);
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.Wage)).IsFalse();
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.Principal)).IsFalse();
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.InsurancePremium)).IsFalse();
        await Assert.That(next.Obligations.Any(o => o.Kind == ObligationKind.InsuranceClaim)).IsFalse();
    }

    [Test]
    public async Task DrawCreditStep_Skips_Surplus_And_Swallows_Draw_Errors()
    {
        var facilityOk = CreditFacilityId.New();
        var facilityBroken = CreditFacilityId.New();
        var state = EconomyState.Empty with
        {
            Period = 1,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.From(100m)),
                [Lender] = new CoreEntity(Lender, CoreEntityKind.Lender, CoreMoney.Zero),
                [Bank] = new CoreEntity(Bank, CoreEntityKind.Bank, CoreMoney.Zero),
            },
            CreditFacilities = new Dictionary<CreditFacilityId, CreditFacility>
            {
                [facilityOk] = new CreditFacility(
                    facilityOk, Lender, Firm, CoreMoney.From(50m), CoreMoney.Zero, IsCommitted: true),
                [facilityBroken] = new CreditFacility(
                    facilityBroken, Lender, Bank, CoreMoney.From(40m), CoreMoney.Zero, IsCommitted: true),
            },
            Obligations =
            [
                new PaymentObligation(
                    ObligationId.New(), Bank, Firm, CoreMoney.From(80m), DuePeriod: 1,
                    ObligationKind.Interest, ObligationStatus.Pending),
            ],
            Policy = StatePolicy.Neutral,
        };

        // Firm has surplus → skip; Bank is short but lender has no cash → Transfer throws → catch/skip.
        var next = new DrawCreditStep().Execute(state);
        await Assert.That(next.CreditFacilities[facilityOk].Drawn.Amount).IsEqualTo(0m);
        await Assert.That(next.CreditFacilities[facilityBroken].Drawn.Amount).IsEqualTo(0m);
        await Assert.That(next.Loans).IsEmpty();
    }

    [Test]
    public async Task TransferOwnershipPaymentsStep_Skips_NonConsumer_And_Missing_Buyer()
    {
        var cohortMissing = new HouseholdCohort(
            CohortId.New(), RegionA, 1, new HouseholdProfile(1m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(20m), HouseholdEntityId: null);
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 1, new HouseholdProfile(1m, 0m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(20m), Household);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.Zero),
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.From(20m)),
            },
            Regions = new Dictionary<RegionId, Region> { [RegionA] = new Region(RegionA, 10, 10m, 10m) },
            Cohorts = new Dictionary<CohortId, HouseholdCohort>
            {
                [cohortMissing.Id] = cohortMissing,
                [cohort.Id] = cohort,
            },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Ore] = new Resource(Ore, "Ore", ResourceKind.IntermediateGood),
                [Food] = new Resource(Food, "Food", ResourceKind.ConsumerGood),
            },
            PostedPrices = new Dictionary<string, PostedPrice>
            {
                ["ore"] = new PostedPrice(RegionA, Ore, CoreMoney.From(2m)),
                ["food"] = new PostedPrice(RegionA, Food, CoreMoney.From(0m)),
            },
            Policy = StatePolicy.Neutral,
        };
        state = HoldingLedger.Credit(state, Firm, RegionA, Ore, 5m);
        state = HoldingLedger.Credit(state, Firm, RegionA, Food, 5m);

        await Assert.That(new TransferOwnershipPaymentsStep().Name).IsEqualTo("08_TransferOwnershipPayments");
        var next = new TransferOwnershipPaymentsStep().Execute(state);
        await Assert.That(HoldingLedger.GetQuantity(next, Household, RegionA, Ore)).IsEqualTo(0m);
        await Assert.That(HoldingLedger.GetQuantity(next, Household, RegionA, Food)).IsEqualTo(0m);
    }

    [Test]
    public async Task TransferEngine_Unknown_Origin_And_Zero_Logistics()
    {
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, 10, 10m, LogisticsCapacity: 0m),
                [RegionB] = new Region(RegionB, 10, 10m, 10m),
            },
            Lanes = new Dictionary<string, TransportLane>
            {
                [TransferEngine.LaneKey(RegionA, RegionB)] =
                    new TransportLane(RegionA, RegionB, TravelPeriods: 1, CapacityPerPeriod: 10m),
            },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Ore] = new Resource(Ore, "Ore", ResourceKind.IntermediateGood),
            },
            Policy = StatePolicy.Neutral,
        };
        state = HoldingLedger.Credit(state, Firm, RegionA, Ore, 5m);

        await Assert.That(() => TransferEngine.StartTransfer(state, Firm, Ore, 1m, RegionId.New(), RegionB))
            .Throws<InvalidOperationException>();
        var blocked = TransferEngine.StartTransfer(state, Firm, Ore, 2m, RegionA, RegionB);
        await Assert.That(blocked.Transfers).IsEmpty();
    }

    [Test]
    public async Task HouseholdConsumeMigrateStep_Skips_NonEntity_And_No_Candidates()
    {
        var linked = new HouseholdCohort(
            CohortId.New(), RegionA, 2, new HouseholdProfile(0.5m, 0m, 1m, 0.9m),
            HouseholdLaborKind.Common, CoreMoney.Zero, Household);
        var unlinked = new HouseholdCohort(
            CohortId.New(), RegionA, 2, new HouseholdProfile(0.5m, 0m, 1m, 0.9m),
            HouseholdLaborKind.Common, CoreMoney.Zero, HouseholdEntityId: null);
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [Household] = new CoreEntity(Household, CoreEntityKind.Household, CoreMoney.Zero),
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, LivingCapacity: 1, 10m, 10m),
            },
            Cohorts = new Dictionary<CohortId, HouseholdCohort>
            {
                [linked.Id] = linked,
                [unlinked.Id] = unlinked,
            },
            Resources = new Dictionary<ResourceId, Resource>
            {
                [Ore] = new Resource(Ore, "Ore", ResourceKind.IntermediateGood),
                [Food] = new Resource(Food, "Food", ResourceKind.ConsumerGood),
            },
            Policy = StatePolicy.Neutral with { HouseholdTaxRate = 0.4m },
        };
        state = HoldingLedger.Credit(state, Household, RegionA, Ore, 4m);
        state = HoldingLedger.Credit(state, Household, RegionA, Food, 0m);

        var next = new HouseholdConsumeMigrateStep().Execute(state);
        await Assert.That(HoldingLedger.GetQuantity(next, Household, RegionA, Ore)).IsEqualTo(4m);
        await Assert.That(next.Cohorts[linked.Id].RegionId).IsEqualTo(RegionA);
    }
}
