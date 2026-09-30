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
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
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
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
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
