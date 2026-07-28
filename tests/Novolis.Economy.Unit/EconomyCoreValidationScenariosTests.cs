using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Steps;
using Novolis.Economy.Core.Transport;
using CoreMoney = Novolis.Economy.Core.Money;
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
        var engine = DefaultPeriodPipeline.CreateEngine();
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
                [WidgetId] = new Resource(WidgetId, "Widget", ResourceKind.ConsumerGood)
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
                [WidgetId] = new Resource(WidgetId, "Widget", ResourceKind.ConsumerGood)
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
                [OreId] = new Resource(OreId, "Ore", ResourceKind.IntermediateGood)
            },
            Lanes = new Dictionary<string, TransportLane>
            {
                [TransferEngine.LaneKey(RegionA, RegionB)] = lane
            }
        };
        return HoldingLedger.Credit(state, FirmId, RegionA, OreId, 10m);
    }
}
