using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
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

public sealed class EconomyCoreExtensionsTests
{
    [Test]
    public async Task Snapshot_Reports_Macro_Stocks()
    {
        var state = ExtensionFixtures.Mixed();
        var snap = state.Snapshot();

        await Assert.That(snap.Period).IsEqualTo(3);
        await Assert.That(snap.EntityCount).IsEqualTo(3);
        await Assert.That(snap.HouseholdCount).IsEqualTo(10);
        await Assert.That(snap.TotalCash.Amount).IsEqualTo(160m);
        await Assert.That(snap.TotalDeposits.Amount).IsEqualTo(25m);
        await Assert.That(snap.BroadMoney.Amount).IsEqualTo(185m);
        await Assert.That(snap.PerformingLoans).IsEqualTo(1);
        await Assert.That(snap.EntitiesByKind[CoreEntityKind.Firm]).IsEqualTo(1);
        await Assert.That(snap.CashByKind[CoreEntityKind.Household].Amount).IsEqualTo(40m);
    }

    [Test]
    public async Task EntityInsight_Flags_Minsky_Illiquid_Solvent()
    {
        var state = ExtensionFixtures.Minsky();
        var insight = state.InsightFor(ExtensionFixtures.FirmId);

        await Assert.That(insight.IsIlliquid).IsTrue();
        await Assert.That(insight.IsInsolventHint).IsFalse();
        await Assert.That(insight.SimpleSolvency.Amount).IsEqualTo(20m);
        await Assert.That(state.IlliquidButSolventEntities().Single()).IsEqualTo(ExtensionFixtures.FirmId);
    }

    [Test]
    public async Task RegionInsight_Reports_Utilization()
    {
        var state = ExtensionFixtures.Mixed();
        var region = state.Regions[ExtensionFixtures.RegionA];
        var insight = region.ToInsight(state);

        await Assert.That(insight.Households).IsEqualTo(10);
        await Assert.That(insight.LivingUtilization).IsEqualTo(0.1m);
        await Assert.That(insight.LaborSupplyHours).IsEqualTo(10m * 12m * 1m);
    }

    [Test]
    public async Task Cohort_ToInsight_Aggregates()
    {
        var state = ExtensionFixtures.Mixed();
        var cohort = state.Cohorts.Values.Single();
        var insight = cohort.ToInsight();

        await Assert.That(insight.TotalCash.Amount).IsEqualTo(100m);
        await Assert.That(insight.EffectiveLaborHours).IsEqualTo(120m);
    }

    [Test]
    public async Task FlowInsight_ObligationBook_CreditBook_Report()
    {
        var state = ExtensionFixtures.Mixed();
        state = state with
        {
            Flows = PeriodFlowLedger.Empty
                .RecordMoneyCreated(CoreMoney.From(5m))
                .RecordWages(CoreMoney.From(2m))
        };

        var flows = state.FlowInsight();
        await Assert.That(flows.MoneyCreated.Amount).IsEqualTo(5m);
        await Assert.That(flows.WagesAccrued.Amount).IsEqualTo(2m);
        await Assert.That(flows.NetMoneyCreated.Amount).IsEqualTo(5m);

        var credit = state.CreditBook();
        await Assert.That(credit.PerformingLoans).IsEqualTo(1);
        await Assert.That(credit.LoanPrincipalOutstanding.Amount).IsEqualTo(25m);

        var minsky = ExtensionFixtures.Minsky();
        var ob = minsky.ObligationBook();
        await Assert.That(ob.PendingCount).IsEqualTo(1);
        await Assert.That(ob.DueNow.Amount).IsEqualTo(100m);
        await Assert.That(ob.PendingSumByKind[CoreObligationKind.Wage].Amount).IsEqualTo(100m);
    }

    [Test]
    public async Task ProjectedAccounts_ValuesHoldingsAndSectors()
    {
        var state = ExtensionFixtures.Mixed();
        var ore = ResourceId.From(Guid.Parse("e4000000-0000-0000-0000-000000000001"));
        state = state with
        {
            Resources = new Dictionary<ResourceId, Resource>
            {
                [ore] = new Resource(
                    ore,
                    "Ore",
                    ResourceKind.IntermediateGood,
                    EconomicAssetId.From(ore.Value))
            },
            Holdings = new Dictionary<string, ResourceHolding>
            {
                [HoldingLedger.Key(ExtensionFixtures.FirmId, ExtensionFixtures.RegionA, ore)] =
                    new ResourceHolding(ExtensionFixtures.FirmId, ExtensionFixtures.RegionA, ore, 10m)
            },
            PostedPrices = new Dictionary<string, PostedPrice>
            {
                [EconomyState.PriceKey(ExtensionFixtures.RegionA, ore)] =
                    new PostedPrice(ExtensionFixtures.RegionA, ore, CoreMoney.From(3m))
            }
        };

        var firmBooks = state.ProjectedBooks(ExtensionFixtures.FirmId);
        await Assert.That(firmBooks.HoldingsValued.Amount).IsEqualTo(30m);
        await Assert.That(firmBooks.Cash.Amount).IsEqualTo(100m);
        await Assert.That(firmBooks.DepositsHeld.Amount).IsEqualTo(25m);
        await Assert.That(firmBooks.LoansPayable.Amount).IsEqualTo(25m);

        var bankBooks = state.ProjectedBooks(ExtensionFixtures.BankId);
        await Assert.That(bankBooks.DepositLiabilities.Amount).IsEqualTo(25m);
        await Assert.That(bankBooks.LoansReceivable.Amount).IsEqualTo(25m);

        var snap = state.ProjectedAccounts();
        await Assert.That(snap.Sectors.Any(s => s.Kind == CoreEntityKind.Firm)).IsTrue();
        await Assert.That(snap.AggregateHoldingsUnpricedQuantity).IsEqualTo(0m);
        await Assert.That(snap.LastPeriod.WagesAccrued.Amount).IsEqualTo(0m);
    }
}
