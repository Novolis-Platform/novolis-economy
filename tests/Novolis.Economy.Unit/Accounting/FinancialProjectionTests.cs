using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Transactions;
using CoreEntity = Novolis.Economy.Core.LegalEntity;
using CoreEntityKind = Novolis.Economy.Core.LegalEntityKind;

namespace Novolis.Economy.Unit.Accounting;

public sealed class FinancialProjectionTests
{
  private static readonly LegalEntityId Firm =
    LegalEntityId.From(Guid.Parse("a1000000-0000-4000-8000-000000000001"));

  private static readonly LegalEntityId Household =
    LegalEntityId.From(Guid.Parse("a1000000-0000-4000-8000-000000000002"));

  private static readonly RegionId Region =
    RegionId.From(Guid.Parse("a1000000-0000-4000-8000-000000000003"));

  private static readonly ResourceId Grain =
    ResourceId.From(Guid.Parse("a1000000-0000-4000-8000-000000000006"));

  private static readonly EconomicAssetId GrainAsset =
    EconomicAssetId.From(Guid.Parse("a1000000-0000-4000-8000-000000000007"));

  private static EconomyState CreateState()
  {
    var state = EconomyState.Empty with
    {
      Entities = new Dictionary<LegalEntityId, CoreEntity>
      {
        [Firm] = new CoreEntity(Firm, CoreEntityKind.Firm, Money.Zero),
        [Household] = new CoreEntity(Household, CoreEntityKind.Household, Money.Zero)
      },
      Regions = new Dictionary<RegionId, Region>
      {
        [Region] = new(Region, 100, 100m, 100m)
      },
      Cohorts = new Dictionary<CohortId, HouseholdCohort>
      {
        [CohortId.From(Guid.Parse("a1000000-0000-4000-8000-000000000004"))] =
          new(
            CohortId.From(Guid.Parse("a1000000-0000-4000-8000-000000000004")),
            Region,
            10,
            new HouseholdProfile(0.5m, 0.1m, 1m, 0m),
            HouseholdLaborKind.Mean,
            Money.Zero,
            Household)
      }
    };

    state = state with
    {
      Resources = new Dictionary<ResourceId, Resource>
      {
        [Grain] = new(Grain, "Grain", ResourceKind.ConsumerGood, GrainAsset)
      }
    };
    state = HoldingLedger.Credit(state, Firm, Region, Grain, 5m);
    state = CashLedger.SetCash(state, Firm, Money.From(100m));
    state = CashLedger.SetCash(state, Household, Money.Zero);
    return state;
  }

  [Test]
  public async Task EntityProjection_IsReadOnly_AndExplainsJournalChanges()
  {
    var state = CreateState();
    var transaction = new EconomicTransaction(
      TransactionId.From(Guid.Parse("a1000000-0000-4000-8000-000000000005")),
      [
        new PositionChange(
          EconomicIdentity.For(Firm),
          state.MonetaryAssetId,
          -25m,
          Region: null),
        new PositionChange(
          EconomicIdentity.For(Household),
          state.MonetaryAssetId,
          25m,
          Region: null)
      ],
      "financial-projection-test");
    var next = EconomicTransactionEngine.Apply(state, transaction);
    var before = next.PositionState;
    var beforeJournalCount = next.Journal.Count;

    var projection = AccountingQuery.Project(
      next,
      new FinancialScope.Entity(Firm));

    await Assert.That(projection.EntityBooks).HasSingleItem();
    await Assert.That(projection.Cash).IsEqualTo(Money.From(75m));
    await Assert.That(projection.Transactions.Count).IsEqualTo(3);
    await Assert.That(projection.Transactions.Any(transaction =>
        transaction.Reason == "financial-projection-test"))
      .IsTrue();
    await Assert.That(projection.MonetaryPositionChange)
      .IsEqualTo(Money.From(75m));
    await Assert.That(next.PositionState).IsEqualTo(before);
    await Assert.That(next.Journal.Count).IsEqualTo(beforeJournalCount);
  }

  [Test]
  public async Task CohortRegionKindAndGroupScopesResolveOwners()
  {
    var state = CreateState();
    var cohortId = state.Cohorts.Keys.Single();

    var cohort = AccountingQuery.Project(
      state,
      new FinancialScope.Cohort(cohortId));
    var region = AccountingQuery.Project(
      state,
      new FinancialScope.Region(Region));
    var kind = AccountingQuery.Project(
      state,
      new FinancialScope.EntityKind(CoreEntityKind.Firm));
    var group = AccountingQuery.Project(
      state,
      new FinancialScope.Group(
        new HashSet<LegalEntityId> { Firm, Household }));

    await Assert.That(cohort.EntityBooks).HasSingleItem();
    await Assert.That(region.EntityBooks.Count).IsEqualTo(2);
    await Assert.That(kind.EntityBooks).HasSingleItem();
    await Assert.That(group.EntityBooks.Count).IsEqualTo(2);
    await Assert.That(group.IsBalanced).IsTrue();
  }
}
