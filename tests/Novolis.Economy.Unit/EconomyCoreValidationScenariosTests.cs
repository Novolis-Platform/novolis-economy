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

/// <summary>
/// Known-economics validation scenarios for Novolis.Economy.Core
/// (Godley–Lavoie SFC, monetary circuit, Minsky, rationing, capacity).
/// </summary>
public sealed class EconomyCoreValidationScenariosTests
{
    /// <summary>
    /// Godley–Lavoie SIM spirit: without bank money creation, sectoral cash is conserved
    /// under fiscal transfer + tax settlement; stocks reconcile under invariants.
    /// </summary>
    [Test]
    public async Task Sfc_Sim_Style_Cash_Conserves_Without_Bank_Money()
    {
        var state = ValidationScenarios.SimStyleOpen();
        var cashBefore = ValidationScenarios.TotalCash(state);
        var depositsBefore = ValidationScenarios.TotalDeposits(state);

        // Opening fiscal: State → Household (money-conserving)
        var engine = DefaultBoundedPeriodPipeline.CreateEngine();
        state = engine.Advance(state);

        await Assert.That(ValidationScenarios.TotalCash(state)).IsEqualTo(cashBefore);
        await Assert.That(ValidationScenarios.TotalDeposits(state)).IsEqualTo(depositsBefore);
        await Assert.That(state.Flows.MoneyCreated.Amount).IsEqualTo(0m);
        await Assert.That(state.Entities[ValidationScenarios.HouseholdId].Cash.Amount)
            .IsEqualTo(110m); // 100 + transfer 10
        await Assert.That(state.Entities[ValidationScenarios.StateId].Cash.Amount)
            .IsEqualTo(90m);

        // Explicit tax obligation settlement (SIM T flow) — still conserves cash
        state = ObligationEngine.Create(
            state,
            ValidationScenarios.HouseholdId,
            ValidationScenarios.StateId,
            CoreMoney.From(15m),
            duePeriod: state.Period,
            CoreObligationKind.Tax);
        state = ObligationEngine.SettleDue(state);

        await Assert.That(ValidationScenarios.TotalCash(state)).IsEqualTo(cashBefore);
        await Assert.That(state.Obligations.Single(o => o.Kind == CoreObligationKind.Tax).Status)
            .IsEqualTo(CoreObligationStatus.Paid);
        InvariantChecker.AssertAll(state);
    }

    /// <summary>
    /// Graziani / circuitist monetary circuit:
    /// bank loan creates deposit → wage paid from deposit → household buys goods → firm repays → deposit destroyed.
    /// </summary>
    [Test]
    public async Task Monetary_Circuit_Creates_Then_Destroys_Deposit()
    {
        var state = ValidationScenarios.CircuitOpen();
        var cashOpen = ValidationScenarios.TotalCash(state);

        // 1. Bank lends to firm (endogenous money)
        state = CreditEngine.OriginateLoan(
            state,
            ValidationScenarios.BankId,
            ValidationScenarios.FirmId,
            CoreMoney.From(100m),
            interestRatePerPeriod: 0m,
            termPeriods: 1);
        var loanId = state.Loans.Keys.Single();
        await Assert.That(DepositLedger.TotalFor(state, ValidationScenarios.FirmId).Amount).IsEqualTo(100m);
        await Assert.That(state.Flows.MoneyCreated.Amount).IsEqualTo(100m);
        await Assert.That(ValidationScenarios.TotalCash(state)).IsEqualTo(cashOpen); // vault cash unchanged

        // 2. Firm pays wages from deposit → household deposit at same bank
        state = ObligationEngine.Create(
            state,
            ValidationScenarios.FirmId,
            ValidationScenarios.HouseholdId,
            CoreMoney.From(60m),
            duePeriod: state.Period,
            CoreObligationKind.Wage);
        state = ObligationEngine.SettleDue(state);
        await Assert.That(DepositLedger.TotalFor(state, ValidationScenarios.FirmId).Amount).IsEqualTo(40m);
        await Assert.That(DepositLedger.TotalFor(state, ValidationScenarios.HouseholdId).Amount).IsEqualTo(60m);
        await Assert.That(ValidationScenarios.TotalDeposits(state)).IsEqualTo(100m); // stock of deposits conserved in transfer

        // 3. Household buys widgets (deposit → firm deposit); goods move
        state = DepositLedger.Debit(state, ValidationScenarios.HouseholdId, ValidationScenarios.BankId, CoreMoney.From(40m));
        state = DepositLedger.Credit(state, ValidationScenarios.FirmId, ValidationScenarios.BankId, CoreMoney.From(40m));
        state = HoldingLedger.TransferOwnership(
            state,
            ValidationScenarios.FirmId,
            ValidationScenarios.HouseholdId,
            ValidationScenarios.RegionA,
            ValidationScenarios.WidgetId,
            4m);
        await Assert.That(DepositLedger.TotalFor(state, ValidationScenarios.FirmId).Amount).IsEqualTo(80m);
        await Assert.That(HoldingLedger.GetQuantity(
            state, ValidationScenarios.HouseholdId, ValidationScenarios.RegionA, ValidationScenarios.WidgetId))
            .IsEqualTo(4m);

        // 4. Firm repays loan → deposit destroyed
        state = CreditEngine.RepayPrincipal(state, loanId, CoreMoney.From(80m));
        await Assert.That(DepositLedger.TotalFor(state, ValidationScenarios.FirmId).Amount).IsEqualTo(0m);
        await Assert.That(state.Loans[loanId].PrincipalOutstanding.Amount).IsEqualTo(20m);
        await Assert.That(state.Flows.MoneyDestroyed.Amount).IsEqualTo(80m);
        await Assert.That(state.Flows.NetMoneyCreated.Amount).IsEqualTo(20m); // 100 created − 80 destroyed
        await Assert.That(ValidationScenarios.TotalDeposits(state)).IsEqualTo(20m); // HH still holds 20
        await Assert.That(ValidationScenarios.TotalCash(state)).IsEqualTo(cashOpen);
        InvariantChecker.AssertAll(state);
    }

    /// <summary>
    /// Minsky: entity can be book-solvent yet illiquid (due-now exceeds accessible means).
    /// </summary>
    [Test]
    public async Task Minsky_Illiquid_But_Solvent()
    {
        var state = ValidationScenarios.MinskyFirm();
        // cash 50, deposits 0, undrawn 0, loans owed 30 → solvency +20
        // due-now wage 100 → surplus −50
        var liq = Liquidity.Of(state, ValidationScenarios.FirmId);
        var solvency = Liquidity.SimpleSolvency(state, ValidationScenarios.FirmId);

        await Assert.That(solvency.Amount).IsEqualTo(20m);
        await Assert.That(liq.Surplus.Amount).IsLessThan(0m);
        await Assert.That(liq.DueNow.Amount).IsEqualTo(100m);

        state = ObligationEngine.SettleDue(state);
        await Assert.That(state.Obligations.Single().Status).IsEqualTo(CoreObligationStatus.Delinquent);
        // Still solvent on the simple book measure after failed settlement
        await Assert.That(Liquidity.SimpleSolvency(state, ValidationScenarios.FirmId).Amount).IsEqualTo(20m);
    }

    /// <summary>
    /// Posted-price matching: sales = min(demand, supply); no oversell; money ↔ goods conserved.
    /// </summary>
    [Test]
    public async Task Posted_Price_Quantity_Rationing()
    {
        var state = ValidationScenarios.RationingMarket();
        var firmWidgetsBefore = HoldingLedger.GetQuantity(
            state, ValidationScenarios.FirmId, ValidationScenarios.RegionA, ValidationScenarios.WidgetId);
        await Assert.That(firmWidgetsBefore).IsEqualTo(3m);

        var cashBefore = ValidationScenarios.TotalCash(state);
        var widgetsBefore = ValidationScenarios.TotalResource(state, ValidationScenarios.WidgetId);

        state = new TransferOwnershipPaymentsStep().Execute(state);

        var bought = HoldingLedger.GetQuantity(
            state, ValidationScenarios.HouseholdId, ValidationScenarios.RegionA, ValidationScenarios.WidgetId);
        var firmLeft = HoldingLedger.GetQuantity(
            state, ValidationScenarios.FirmId, ValidationScenarios.RegionA, ValidationScenarios.WidgetId);

        // Unit price 10; HH cash 1000 could demand 100, but only 3 available
        await Assert.That(bought).IsEqualTo(3m);
        await Assert.That(firmLeft).IsEqualTo(0m);
        await Assert.That(state.Entities[ValidationScenarios.HouseholdId].Cash.Amount).IsEqualTo(970m);
        await Assert.That(state.Entities[ValidationScenarios.FirmId].Cash.Amount).IsEqualTo(30m);
        await Assert.That(ValidationScenarios.TotalCash(state)).IsEqualTo(cashBefore);
        await Assert.That(ValidationScenarios.TotalResource(state, ValidationScenarios.WidgetId))
            .IsEqualTo(widgetsBefore);
    }

    /// <summary>
    /// Capacity binds: living overcrowd is an invariant failure; logistics clamps transfer qty.
    /// </summary>
    [Test]
    public async Task Capacity_Binds_Living_And_Logistics()
    {
        // Living capacity
        var crowded = ValidationScenarios.OvercrowdedRegion();
        var livingViolations = InvariantChecker.Check(crowded);
        await Assert.That(livingViolations.Any(v => v.Code == "CAP_LIVE")).IsTrue();

        // Logistics clamp: lane/region capacity 4, try to ship 10
        var state = ValidationScenarios.TightLogistics();
        state = TransferEngine.StartTransfer(
            state,
            ValidationScenarios.FirmId,
            ValidationScenarios.OreId,
            quantity: 10m,
            ValidationScenarios.RegionA,
            ValidationScenarios.RegionB);

        await Assert.That(state.Transfers.Count).IsEqualTo(1);
        await Assert.That(state.Transfers[0].Quantity).IsEqualTo(4m);
        await Assert.That(HoldingLedger.GetQuantity(
            state, ValidationScenarios.FirmId, ValidationScenarios.RegionA, ValidationScenarios.OreId))
            .IsEqualTo(6m); // 10 − 4 shipped
        InvariantChecker.AssertAll(state);
    }
}
