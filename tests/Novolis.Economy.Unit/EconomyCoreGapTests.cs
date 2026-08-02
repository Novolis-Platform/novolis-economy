using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Markets;
using Novolis.Economy.Markets.Extensions;
using Novolis.Economy.Population;
using Novolis.Economy.Population.Extensions;
using CoreMoney = Novolis.Economy.Core.Money;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;

namespace Novolis.Economy.Unit;

public sealed class EconomyCoreGapTests
{
    [Test]
    public async Task Money_OperatorsAndCompare()
    {
        var a = CoreMoney.From(10m);
        var b = CoreMoney.From(3m);
        await Assert.That((a + b).Amount).IsEqualTo(13m);
        await Assert.That((a - b).Amount).IsEqualTo(7m);
        await Assert.That((a * 2m).Amount).IsEqualTo(20m);
        await Assert.That(a > b).IsTrue();
        await Assert.That(a.CompareTo(b)).IsGreaterThan(0);
        await Assert.That(CoreMoney.Zero.ToString()).IsEqualTo("0");
    }

    [Test]
    public async Task EntityRules_EnforcesShareIssuance()
    {
        await Assert.That(EntityRules.IsOwnable(CoreEntityKind.Firm)).IsTrue();
        await Assert.That(EntityRules.MayOperateActivity(CoreEntityKind.Firm)).IsTrue();
        await Assert.That(EntityRules.MayAcceptDeposits(CoreEntityKind.Bank)).IsTrue();
        await Assert.That(EntityRules.IsPolicyAuthority(CoreEntityKind.State)).IsTrue();

        var household = new CoreEntity(LegalEntityId.New(), CoreEntityKind.Household, CoreMoney.Zero);
        await Assert.That(() => EntityRules.EnsureMayIssueShares(household)).Throws<InvalidOperationException>();
        await Assert.That(() => EntityRules.EnsureOwnableIssuer(household)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ObservedMarketBookExtensions_BuildSnapshot()
    {
        var book = new ObservedMarketBook();
        var product = ProductId.From(Guid.NewGuid());
        book.RecordTrade(product, Quantity.From(2m), Money.From(5m), SimulationHour.Epoch);
        book.RecordTrade(product, Quantity.From(1m), Money.From(6m), new SimulationHour(1));

        var insight = book.ToInsight(product);
        await Assert.That(insight).IsNotNull();
        await Assert.That(insight!.LastPrice.Amount).IsEqualTo(6m);

        var snapshot = book.Snapshot();
        await Assert.That(snapshot.ProductCount).IsEqualTo(1);
        await Assert.That(snapshot.TotalTrades).IsEqualTo(2);
    }

    [Test]
    public async Task ConsumerCohortExtensions_BuildInsight()
    {
        var cohort = new ConsumerCohort(
            ConsumerCohortId.From(Guid.NewGuid()),
            new PopulationCount(500),
            Money.From(100m),
            new PreferenceProfile(ImmutableArray<CategoryPreference>.Empty, 1m, 1m, 0.5m),
            GeographicAreaId.From(Guid.NewGuid()));

        var insight = cohort.ToInsight();
        await Assert.That(insight.HouseholdCount).IsEqualTo(500);
        await Assert.That(insight.DisposableIncome.Amount).IsEqualTo(100m);
    }

    [Test]
    public async Task LegalEntityExtensions_ReportsLiquidity()
    {
        var id = LegalEntityId.New();
        var entity = new CoreEntity(id, CoreEntityKind.Firm, CoreMoney.From(42m));
        var state = EconomyState.Empty with
        {
            Entities = new Dictionary<LegalEntityId, CoreEntity> { [id] = entity },
        };

        var liquidity = entity.Liquidity(state);
        await Assert.That(liquidity.Cash.Amount).IsEqualTo(42m);
        await Assert.That(entity.IsIlliquid(state)).IsFalse();
    }
}
