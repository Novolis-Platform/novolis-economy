using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Steps;
using Novolis.Economy.Core.Transport;
using CoreMoney = Novolis.Economy.Core.Money;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;
using CoreObligationKind = Novolis.Economy.Core.ObligationKind;
using CoreObligationStatus = Novolis.Economy.Core.ObligationStatus;

namespace Novolis.Economy.Unit;

/// <summary>
/// Realistic multi-period fiscal / labour / credit / rationing dynamics
/// suitable for nation-host loops (Civics bridge, geopolitics shortage feedback).
/// </summary>
public sealed class KnownDynamicsFiscalLaborScenariosTests
{
    [Test]
    public async Task Household_Tax_Drains_Cash_To_State_Conserving_Total()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.Open(hhCash: 200m, stateCash: 50m, transfer: 0m, hhTax: 0.10m);
        var cash0 = state.TotalCash().Amount;

        state = engine.Advance(state);

        await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
        // Tax obligation created on opening HH cash after transfer step (transfer=0)
        await Assert.That(state.Flows.TaxCollected.Amount).IsEqualTo(20m); // 200 * 0.10
        var tax = state.Obligations.Single(o => o.Kind == CoreObligationKind.Tax);
        await Assert.That(tax.Status).IsEqualTo(CoreObligationStatus.Paid);
        await Assert.That(state.Entities[FiscalNation.HouseholdId].Cash.Amount).IsEqualTo(180m);
        await Assert.That(state.Entities[FiscalNation.StateId].Cash.Amount).IsEqualTo(70m);
        InvariantChecker.AssertAll(state);
    }

    [Test]
    public async Task Firm_Tax_And_Household_Tax_Both_Fund_Treasury()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.Open(hhCash: 100m, stateCash: 0m, transfer: 0m, hhTax: 0.2m, firmCash: 50m, firmTax: 0.1m);
        var cash0 = state.TotalCash().Amount;
        state = engine.Advance(state);
        await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
        // HH 20 + Firm 5
        await Assert.That(state.Flows.TaxCollected.Amount).IsEqualTo(25m);
        await Assert.That(state.Entities[FiscalNation.StateId].Cash.Amount).IsEqualTo(25m);
    }

    [Test]
    public async Task Transfer_Then_Tax_Same_Period_Conserves_And_Orders_Correctly()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        // State 100 → transfer 20 → HH 120; then tax 10% of HH cash → 12 tax
        var state = FiscalNation.Open(hhCash: 100m, stateCash: 100m, transfer: 20m, hhTax: 0.10m);
        var cash0 = state.TotalCash().Amount;
        state = engine.Advance(state);
        await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
        await Assert.That(state.Flows.TransfersPaid.Amount).IsEqualTo(20m);
        await Assert.That(state.Flows.TaxCollected.Amount).IsEqualTo(12m); // 120 * 0.1
        await Assert.That(state.Entities[FiscalNation.HouseholdId].Cash.Amount).IsEqualTo(108m);
        await Assert.That(state.Entities[FiscalNation.StateId].Cash.Amount).IsEqualTo(92m); // 100-20+12
    }

    [Test]
    public async Task Multi_Household_Cohort_Transfer_Is_All_Or_Nothing_Then_Stalls()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        // 2 households × 10 = 20 needed; state 25 funds once, then stalls with 5 left
        var state = FiscalNation.Open(hhCash: 10m, stateCash: 25m, transfer: 10m, hhTax: 0m);
        // bump cohort size to 2 (same entity still receives total)
        var cohort = state.Cohorts.Values.Single();
        state = state with
        {
            Cohorts = new Dictionary<CohortId, HouseholdCohort>
            {
                [cohort.Id] = cohort with { HouseholdCount = 2 },
            },
        };
        var cash0 = state.TotalCash().Amount;
        state = engine.Advance(state);
        await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
        await Assert.That(state.Flows.TransfersPaid.Amount).IsEqualTo(20m);
        await Assert.That(state.Entities[FiscalNation.StateId].Cash.Amount).IsEqualTo(5m);

        state = engine.Advance(state);
        await Assert.That(state.Flows.TransfersPaid.Amount).IsEqualTo(0m);
        await Assert.That(state.Entities[FiscalNation.StateId].Cash.Amount).IsEqualTo(5m);
    }

    [Test]
    public async Task Production_Accrues_Wages_And_Settles_To_Household()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.Factory(wage: 1m, laborHours: 1m, capacity: 3m, ore: 100m);
        var cash0 = state.TotalCash().Amount;
        state = engine.Advance(state);
        await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
        await Assert.That(state.Flows.WagesAccrued.Amount).IsGreaterThan(0m);
        await Assert.That(HoldingLedger.GetQuantity(
            state, FiscalNation.FirmId, FiscalNation.RegionA, FiscalNation.WidgetId))
            .IsGreaterThan(0m);
        await Assert.That(state.Entities[FiscalNation.HouseholdId].Cash.Amount).IsGreaterThan(0m);
    }

    [Test]
    public async Task Twelve_Period_Fiscal_Loop_Never_Creates_Bank_Money()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.Open(hhCash: 80m, stateCash: 120m, transfer: 5m, hhTax: 0.05m, firmCash: 40m, firmTax: 0.02m);
        var cash0 = state.TotalCash().Amount;
        for (var i = 0; i < 12; i++)
        {
            state = engine.Advance(state);
            await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
            await Assert.That(state.Flows.MoneyCreated.Amount).IsEqualTo(0m);
            await Assert.That(state.Flows.MoneyDestroyed.Amount).IsEqualTo(0m);
            InvariantChecker.AssertAll(state);
        }

        // State should have absorbed some tax net of transfers over the year
        await Assert.That(state.Entities[FiscalNation.StateId].Cash.Amount).IsGreaterThan(0m);
    }

    [Test]
    public async Task High_Tax_Regime_Accumulates_More_State_Cash_Than_Low_Tax()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var high = FiscalNation.Open(hhCash: 200m, stateCash: 50m, transfer: 0m, hhTax: 0.25m);
        var low = FiscalNation.Open(hhCash: 200m, stateCash: 50m, transfer: 0m, hhTax: 0.05m);
        for (var i = 0; i < 6; i++)
        {
            high = engine.Advance(high);
            low = engine.Advance(low);
        }

        await Assert.That(high.Entities[FiscalNation.StateId].Cash.Amount)
            .IsGreaterThan(low.Entities[FiscalNation.StateId].Cash.Amount);
        await Assert.That(high.Entities[FiscalNation.HouseholdId].Cash.Amount)
            .IsLessThan(low.Entities[FiscalNation.HouseholdId].Cash.Amount);
    }

    [Test]
    public async Task Generous_Transfers_Redistribute_Until_Treasury_Thins()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.Open(hhCash: 10m, stateCash: 100m, transfer: 15m, hhTax: 0m);
        var hhPath = new List<decimal>();
        for (var i = 0; i < 8; i++)
        {
            state = engine.Advance(state);
            hhPath.Add(state.Entities[FiscalNation.HouseholdId].Cash.Amount);
        }

        // First periods rise by 15; eventually flat when state cannot fund
        await Assert.That(hhPath[0]).IsEqualTo(25m);
        await Assert.That(hhPath[1]).IsEqualTo(40m);
        await Assert.That(hhPath[^1]).IsEqualTo(hhPath[^2]); // stalled
        await Assert.That(state.Entities[FiscalNation.StateId].Cash.Amount).IsLessThan(15m);
    }

    [Test]
    public async Task Endogenous_Loan_Raises_Deposits_Without_Vault_Cash_Change()
    {
        var state = FiscalNation.BankCircuit();
        var vault0 = state.Entities[FiscalNation.BankId].Cash.Amount;
        state = CreditEngine.OriginateLoan(
            state, FiscalNation.BankId, FiscalNation.FirmId, CoreMoney.From(50m), 0m, 2);
        await Assert.That(state.Flows.MoneyCreated.Amount).IsEqualTo(50m);
        await Assert.That(DepositLedger.TotalFor(state, FiscalNation.FirmId).Amount).IsEqualTo(50m);
        await Assert.That(state.Entities[FiscalNation.BankId].Cash.Amount).IsEqualTo(vault0);
    }

    [Test]
    public async Task Loan_Repayment_Destroys_Deposits_Symmetrically()
    {
        var state = FiscalNation.BankCircuit();
        state = CreditEngine.OriginateLoan(
            state, FiscalNation.BankId, FiscalNation.FirmId, CoreMoney.From(40m), 0m, 1);
        var loanId = state.Loans.Keys.Single();
        state = CreditEngine.RepayPrincipal(state, loanId, CoreMoney.From(40m));
        await Assert.That(state.Flows.MoneyDestroyed.Amount).IsEqualTo(40m);
        await Assert.That(state.Flows.NetMoneyCreated.Amount).IsEqualTo(0m);
        await Assert.That(DepositLedger.TotalFor(state, FiscalNation.FirmId).Amount).IsEqualTo(0m);
    }

    [Test]
    public async Task Interest_Obligation_Created_Each_Period_On_Performing_Loan()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.BankLoanInterest(principal: 100m, rate: 0.05m);
        var deposits0 = state.TotalDeposits().Amount;
        state = engine.Advance(state);
        // Deposit stock conserved under interest reassignment (borrower → bank claim)
        await Assert.That(state.TotalDeposits().Amount).IsEqualTo(deposits0);
        var interest = state.Obligations.Where(o => o.Kind == CoreObligationKind.Interest).ToList();
        await Assert.That(interest.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(interest.All(o => o.Amount.Amount == 5m)).IsTrue();
    }

    [Test]
    public async Task Posted_Price_Cash_Rationing_When_Supply_Abundant()
    {
        // HH has only 25 cash; price 10 → can buy 2.5 of 10 widgets (fractional fill)
        var state = FiscalNation.Market(widgets: 10m, hhCash: 25m, price: 10m);
        var cash0 = state.TotalCash().Amount;
        state = new TransferOwnershipPaymentsStep().Execute(state);
        await Assert.That(HoldingLedger.GetQuantity(
            state, FiscalNation.HouseholdId, FiscalNation.RegionA, FiscalNation.WidgetId))
            .IsEqualTo(2.5m);
        await Assert.That(state.Entities[FiscalNation.HouseholdId].Cash.Amount).IsEqualTo(0m);
        await Assert.That(HoldingLedger.GetQuantity(
            state, FiscalNation.FirmId, FiscalNation.RegionA, FiscalNation.WidgetId))
            .IsEqualTo(7.5m);
        await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
    }

    [Test]
    public async Task Carriage_Preserves_Owner_Across_Travel()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.Lane(travel: 2, qty: 6m);
        state = TransferEngine.StartTransfer(
            state, FiscalNation.FirmId, FiscalNation.OreId, 6m,
            FiscalNation.RegionA, FiscalNation.RegionB);
        state = engine.Advance(state);
        await Assert.That(state.Transfers.Count).IsEqualTo(1);
        state = engine.Advance(state);
        await Assert.That(HoldingLedger.GetQuantity(
            state, FiscalNation.FirmId, FiscalNation.RegionB, FiscalNation.OreId))
            .IsEqualTo(6m);
    }

    [Test]
    public async Task Zero_Policy_Is_Quiet_Period_With_Invariant_Hold()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.Open(hhCash: 10m, stateCash: 10m, transfer: 0m, hhTax: 0m);
        var cash0 = state.TotalCash().Amount;
        state = engine.Advance(state);
        await Assert.That(state.Flows.TaxCollected.Amount).IsEqualTo(0m);
        await Assert.That(state.Flows.TransfersPaid.Amount).IsEqualTo(0m);
        await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
        InvariantChecker.AssertAll(state);
    }

    [Test]
    public async Task Tax_Base_Uses_Cash_After_Transfer_Not_Opening_Stock()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.Open(hhCash: 0m, stateCash: 50m, transfer: 40m, hhTax: 0.25m);
        state = engine.Advance(state);
        // After transfer HH=40; tax = 10
        await Assert.That(state.Flows.TaxCollected.Amount).IsEqualTo(10m);
        await Assert.That(state.Entities[FiscalNation.HouseholdId].Cash.Amount).IsEqualTo(30m);
    }

    [Test]
    public async Task Delinquent_Wage_Ages_To_Default_While_Cash_Conserved()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.UnpayableWage();
        var cash0 = state.TotalCash().Amount;
        state = engine.Advance(state); // delinquent
        state = engine.Advance(state);
        state = engine.Advance(state); // defaulted
        var wage = state.Obligations.Single(o => o.Kind == CoreObligationKind.Wage);
        await Assert.That(wage.Status).IsEqualTo(CoreObligationStatus.Defaulted);
        await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
    }

    [Test]
    public async Task Insurance_Premium_Is_Money_Conserving_Obligation()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.InsuredFirm(premium: 8m);
        var cash0 = state.TotalCash().Amount;
        state = engine.Advance(state);
        await Assert.That(state.TotalCash().Amount).IsEqualTo(cash0);
        var prem = state.Obligations.Single(o => o.Kind == CoreObligationKind.InsurancePremium);
        await Assert.That(prem.Status).IsEqualTo(CoreObligationStatus.Paid);
        await Assert.That(state.Entities[FiscalNation.FirmId].Cash.Amount).IsEqualTo(92m);
        await Assert.That(state.Entities[FiscalNation.InsurerId].Cash.Amount).IsEqualTo(58m);
    }

    [Test]
    public async Task Nation_Host_Style_Delivery_Totals_Match_Flow_Ledger()
    {
        // Pattern Civics.EconomyBridge expects: read Flows after Advance, feed civic context.
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.Open(hhCash: 150m, stateCash: 80m, transfer: 12m, hhTax: 0.08m);
        state = engine.Advance(state);
        var tax = (double)state.Flows.TaxCollected.Amount;
        var transfers = (double)state.Flows.TransfersPaid.Amount;
        await Assert.That(tax).IsGreaterThan(0);
        await Assert.That(transfers).IsEqualTo(12.0);
        // Delivery facts are non-negative and finite for PeriodContextFromDelivery
        await Assert.That(tax).IsGreaterThanOrEqualTo(0);
        await Assert.That(transfers).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Capacity_Violation_Surfaces_Living_Overcrowd()
    {
        var region = new Region(FiscalNation.RegionA, LivingCapacity: 1, 100m, 100m);
        var cohort = new HouseholdCohort(
            CohortId.New(), FiscalNation.RegionA, HouseholdCount: 4,
            new HouseholdProfile(0.5m, 0.2m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(1m));
        var state = EconomyState.Empty with
        {
            Regions = new Dictionary<RegionId, Region> { [FiscalNation.RegionA] = region },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
        };
        var v = InvariantChecker.Check(state);
        await Assert.That(v.Any(x => x.Code == "CAP_LIVE")).IsTrue();
    }

    [Test]
    public async Task Logistics_Capacity_Clamps_Shipment_Quantity()
    {
        var state = FiscalNation.Lane(travel: 1, qty: 20m, logisticsCap: 3m);
        state = HoldingLedger.Upsert(state, FiscalNation.FirmId, FiscalNation.RegionA, FiscalNation.OreId, 20m);
        state = TransferEngine.StartTransfer(
            state, FiscalNation.FirmId, FiscalNation.OreId, 20m,
            FiscalNation.RegionA, FiscalNation.RegionB);
        await Assert.That(state.Transfers.Single().Quantity).IsEqualTo(3m);
    }

    [Test]
    public async Task Three_Sector_Closed_Economy_Cash_Identity_Holds_Year()
    {
        var engine = DefaultPeriodPipeline.CreateEngine();
        var state = FiscalNation.ThreeSector(hh: 60m, firm: 80m, stateCash: 40m, bank: 20m);
        var cash0 = state.TotalCash().Amount;
        await Assert.That(cash0).IsEqualTo(200m);
        for (var i = 0; i < 12; i++)
        {
            state = engine.Advance(state);
            await Assert.That(state.TotalCash().Amount).IsEqualTo(200m);
            InvariantChecker.AssertAll(state);
        }
    }
}

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
                [OreId] = new Resource(OreId, "Ore", ResourceKind.IntermediateGood),
                [WidgetId] = new Resource(WidgetId, "Widget", ResourceKind.ConsumerGood),
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
                [WidgetId] = new Resource(WidgetId, "Widget", ResourceKind.ConsumerGood),
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
                [OreId] = new Resource(OreId, "Ore", ResourceKind.IntermediateGood),
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
