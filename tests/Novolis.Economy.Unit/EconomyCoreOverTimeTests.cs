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

/// <summary>
/// Multi-period behavioural tests for known Core dynamics
/// (conservation, carriage lag, delinquency aging, production, endogenous interest drain).
/// </summary>
public sealed class EconomyCoreOverTimeTests
{
    [Test]
    public async Task Period_Advances_Monotonically_Over_Horizon()
    {
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
        var state = EconomyState.Empty;
        for (var i = 1; i <= 12; i++)
        {
            state = engine.Advance(state);
            await Assert.That(state.Period).IsEqualTo(i);
            await Assert.That(state.Snapshot().Period).IsEqualTo(i);
        }
    }

    [Test]
    public async Task Sfc_Cash_Conserves_Across_Many_Periods_Without_Bank_Money()
    {
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
        var state = OverTimeFixtures.ClosedFiscalNoTransfer();
        var cash0 = state.TotalCash().Amount;
        var deposits0 = state.TotalDeposits().Amount;

        for (var i = 0; i < 10; i++)
        {
            state = engine.Advance(state);
            await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
            await Assert.That(state.TotalDeposits().Amount).IsEqualTo(deposits0);
            await Assert.That(state.Flows.MoneyCreated.Amount).IsEqualTo(0m);
            await Assert.That(state.Flows.MoneyDestroyed.Amount).IsEqualTo(0m);
            InvariantChecker.AssertAll(state);
        }
    }

    [Test]
    public async Task Transfer_Arrives_After_Travel_Periods()
    {
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
        var state = OverTimeFixtures.SlowLane(travelPeriods: 3);
        state = TransferEngine.StartTransfer(
            state,
            OverTimeFixtures.FirmId,
            OverTimeFixtures.OreId,
            quantity: 8m,
            OverTimeFixtures.RegionA,
            OverTimeFixtures.RegionB);

        await Assert.That(state.Transfers.Single().RemainingPeriods).IsEqualTo(3);
        await Assert.That(HoldingLedger.GetQuantity(
            state, OverTimeFixtures.FirmId, OverTimeFixtures.RegionA, OverTimeFixtures.OreId))
            .IsEqualTo(2m);

        // Period 1: Remaining 2 — still in flight
        state = engine.Advance(state);
        await Assert.That(state.Transfers.Count).IsEqualTo(1);
        await Assert.That(state.Transfers[0].RemainingPeriods).IsEqualTo(2);
        await Assert.That(HoldingLedger.GetQuantity(
            state, OverTimeFixtures.FirmId, OverTimeFixtures.RegionB, OverTimeFixtures.OreId))
            .IsEqualTo(0m);

        // Period 2: Remaining 1
        state = engine.Advance(state);
        await Assert.That(state.Transfers[0].RemainingPeriods).IsEqualTo(1);

        // Period 3: arrives
        state = engine.Advance(state);
        await Assert.That(state.Transfers.Count).IsEqualTo(0);
        await Assert.That(HoldingLedger.GetQuantity(
            state, OverTimeFixtures.FirmId, OverTimeFixtures.RegionB, OverTimeFixtures.OreId))
            .IsEqualTo(8m);
        // Ownership preserved across the horizon
        await Assert.That(HoldingLedger.GetQuantity(
            state, OverTimeFixtures.FirmId, OverTimeFixtures.RegionA, OverTimeFixtures.OreId))
            .IsEqualTo(2m);
    }

    [Test]
    public async Task Delinquent_Obligation_Defaults_After_Aging()
    {
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
        // Due at period 1; firm cannot pay
        var state = OverTimeFixtures.UnpayableWageDueAt(duePeriod: 1);

        state = engine.Advance(state); // → period 1: unpaid → Delinquent
        await Assert.That(state.Period).IsEqualTo(1);
        var ob = state.Obligations.Single(o => o.Kind == CoreObligationKind.Wage);
        await Assert.That(ob.Status).IsEqualTo(CoreObligationStatus.Delinquent);

        state = engine.Advance(state); // → period 2: still delinquent (age 1)
        ob = state.Obligations.Single(o => o.Kind == CoreObligationKind.Wage);
        await Assert.That(ob.Status).IsEqualTo(CoreObligationStatus.Delinquent);

        state = engine.Advance(state); // → period 3: age ≥ 2 → Defaulted
        ob = state.Obligations.Single(o => o.Kind == CoreObligationKind.Wage);
        await Assert.That(ob.Status).IsEqualTo(CoreObligationStatus.Defaulted);
        await Assert.That(state.Period - ob.DuePeriod).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Production_Accumulates_Output_Over_Periods()
    {
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
        var state = new BoundedPeriodState(OverTimeFixtures.SteadyProduction());
        var widgets = new List<decimal>();

        for (var i = 0; i < 5; i++)
        {
            // Replenish ore so input is never the bottleneck
            state = HoldingLedger.Upsert(
                state,
                OverTimeFixtures.FirmId,
                OverTimeFixtures.RegionA,
                OverTimeFixtures.OreId,
                100m);
            state = engine.Advance(state);
            widgets.Add(HoldingLedger.GetQuantity(
                state,
                OverTimeFixtures.FirmId,
                OverTimeFixtures.RegionA,
                OverTimeFixtures.WidgetId));
        }

        // Strictly non-decreasing output stock; at least some production occurred
        for (var i = 1; i < widgets.Count; i++)
            await Assert.That(widgets[i]).IsGreaterThanOrEqualTo(widgets[i - 1]);
        await Assert.That(widgets[^1]).IsGreaterThan(0m);
        await Assert.That(state.Scratch.ActualRuns.Values.Sum()).IsGreaterThan(0m);
    }

    [Test]
    public async Task Bank_Interest_Drains_Borrower_Deposit_Over_Periods_Conserving_Deposits()
    {
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
        var state = OverTimeFixtures.BankLoanWithInterest();
        var loanId = state.Loans.Keys.Single();
        var deposits0 = state.TotalDeposits().Amount;
        await Assert.That(deposits0).IsEqualTo(100m);

        var firmDeposits = new List<decimal>();
        var bankClaims = new List<decimal>(); // lender bank's view: deposits owed to firm + any to bank itself

        for (var i = 0; i < 4; i++)
        {
            state = engine.Advance(state);
            // No vault-cash creation; deposit stock conserved when interest is paid bank←borrower
            await Assert.That(state.TotalDeposits().Amount).IsEqualTo(deposits0);
            await Assert.That(state.TotalCash().Amount).IsEqualTo(OverTimeFixtures.BankLoanCashOpen);
            firmDeposits.Add(DepositLedger.TotalFor(state, OverTimeFixtures.FirmId).Amount);
            bankClaims.Add(DepositLedger.TotalFor(state, OverTimeFixtures.BankId).Amount);
            InvariantChecker.AssertAll(state);
        }

        // Interest each period moves deposit claims toward the bank (as creditor)
        await Assert.That(firmDeposits[^1]).IsLessThan(firmDeposits[0]);
        await Assert.That(bankClaims[^1]).IsGreaterThan(0m);
        // Loan still performing with principal intact (interest-only path)
        await Assert.That(state.Loans[loanId].PrincipalOutstanding.Amount).IsEqualTo(100m);
        await Assert.That(state.Loans[loanId].Status).IsEqualTo(CoreLoanStatus.Performing);
    }

    [Test]
    public async Task Fiscal_Transfer_Each_Period_Until_State_Cash_Exhausted()
    {
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
        // State has 25 cash; transfer 10/period → three successful transfers then stall
        var state = OverTimeFixtures.FiscalTransfer(stateCash: 25m, transferPerHh: 10m);
        var cash0 = state.TotalCash().Amount;
        var hhPath = new List<decimal>();
        var statePath = new List<decimal>();

        for (var i = 0; i < 5; i++)
        {
            state = engine.Advance(state);
            await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
            hhPath.Add(state.Entities[OverTimeFixtures.HouseholdId].Cash.Amount);
            statePath.Add(state.Entities[OverTimeFixtures.StateId].Cash.Amount);
        }

        // First two periods move 10 each (25 → 15 → 5); third cannot fund another 10
        await Assert.That(hhPath[0]).IsEqualTo(110m);
        await Assert.That(hhPath[1]).IsEqualTo(120m);
        await Assert.That(statePath[1]).IsEqualTo(5m);
        // Later periods cannot transfer a full 10 — household cash flat after exhaustion
        await Assert.That(hhPath[2]).IsEqualTo(hhPath[1]);
        await Assert.That(hhPath[3]).IsEqualTo(hhPath[1]);
        await Assert.That(hhPath[4]).IsEqualTo(hhPath[1]);
    }

    [Test]
    public async Task Illiquid_But_Solvent_Persists_Until_Obligation_Defaults()
    {
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
        // Period already 1 so the wage is due-now before settlement
        var state = OverTimeFixtures.MinskyHorizon() with { Period = 1 };

        var insight = state.InsightFor(OverTimeFixtures.FirmId);
        await Assert.That(insight.IsIlliquid).IsTrue();
        await Assert.That(insight.IsInsolventHint).IsFalse();
        await Assert.That(state.IlliquidButSolventEntities()).Contains(OverTimeFixtures.FirmId);

        state = engine.Advance(state); // → 2: settle fails → Delinquent
        var wage = state.Obligations.Single(o => o.Kind == CoreObligationKind.Wage);
        await Assert.That(wage.Status).IsEqualTo(CoreObligationStatus.Delinquent);
        await Assert.That(state.InsightFor(OverTimeFixtures.FirmId).SimpleSolvency.Amount).IsEqualTo(20m);

        state = engine.Advance(state); // → 3: age 2 → Defaulted (due was 1)
        wage = state.Obligations.Single(o => o.Kind == CoreObligationKind.Wage);
        await Assert.That(wage.Status).IsEqualTo(CoreObligationStatus.Defaulted);
        await Assert.That(state.InsightFor(OverTimeFixtures.FirmId).SimpleSolvency.Amount).IsEqualTo(20m);
    }
}
