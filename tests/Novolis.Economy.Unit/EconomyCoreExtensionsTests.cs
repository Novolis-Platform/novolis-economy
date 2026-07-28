using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Finance;
using CoreMoney = Novolis.Economy.Core.Money;
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
}

file static class ExtensionFixtures
{
    public static readonly RegionId RegionA = RegionId.From(Guid.Parse("e1000000-0000-0000-0000-000000000001"));
    public static readonly LegalEntityId FirmId = LegalEntityId.From(Guid.Parse("e2000000-0000-0000-0000-000000000001"));
    public static readonly LegalEntityId HouseholdId = LegalEntityId.From(Guid.Parse("e2000000-0000-0000-0000-000000000002"));
    public static readonly LegalEntityId BankId = LegalEntityId.From(Guid.Parse("e2000000-0000-0000-0000-000000000003"));
    public static readonly LegalEntityId LenderId = LegalEntityId.From(Guid.Parse("e2000000-0000-0000-0000-000000000004"));

    public static EconomyState Mixed()
    {
        var cohort = new HouseholdCohort(
            CohortId.New(), RegionA, 10,
            new HouseholdProfile(0.5m, 0.2m, 1m, 0m),
            HouseholdLaborKind.Common, CoreMoney.From(10m), HouseholdId);

        return EconomyState.Empty with
        {
            Period = 3,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(100m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.From(40m)),
                [BankId] = new CoreEntity(BankId, CoreEntityKind.Bank, CoreMoney.From(20m))
            },
            Regions = new Dictionary<RegionId, Region>
            {
                [RegionA] = new Region(RegionA, LivingCapacity: 100, ProductionCapacity: 50m, LogisticsCapacity: 20m)
            },
            Cohorts = new Dictionary<CohortId, HouseholdCohort> { [cohort.Id] = cohort },
            Deposits =
            [
                new Deposit(FirmId, BankId, CoreMoney.From(25m))
            ],
            Loans = CreatePerformingLoan(BankId, FirmId, CoreMoney.From(25m))
        };
    }

    private static Dictionary<CoreLoanId, CoreLoan> CreatePerformingLoan(
        LegalEntityId lender,
        LegalEntityId borrower,
        CoreMoney principal)
    {
        var id = CoreLoanId.New();
        return new Dictionary<CoreLoanId, CoreLoan>
        {
            [id] = new CoreLoan(id, lender, borrower, principal, 0.01m, 4, CoreLoanStatus.Performing)
        };
    }

    public static EconomyState Minsky()
    {
        var loanId = CoreLoanId.From(Guid.Parse("e3000000-0000-0000-0000-000000000001"));
        return EconomyState.Empty with
        {
            Period = 1,
            Entities = new Dictionary<LegalEntityId, CoreEntity>
            {
                [FirmId] = new CoreEntity(FirmId, CoreEntityKind.Firm, CoreMoney.From(50m)),
                [LenderId] = new CoreEntity(LenderId, CoreEntityKind.Lender, CoreMoney.From(100m)),
                [HouseholdId] = new CoreEntity(HouseholdId, CoreEntityKind.Household, CoreMoney.Zero)
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
}
