using Novolis.Economy.Core;
using Novolis.Economy.Core.Extensions;
using Novolis.Economy.Core.Transactions;

namespace Novolis.Economy.Accounting;

/// <summary>Pure read queries over Core economic state and journal history.</summary>
public static class AccountingQuery
{
  /// <summary>Projects financials for one scope.</summary>
  public static FinancialProjection Project(
    EconomyState state,
    FinancialScope scope,
    int? period = null)
  {
    ArgumentNullException.ThrowIfNull(state);
    ArgumentNullException.ThrowIfNull(scope);

    var entityIds = ResolveEntityIds(state, scope);
    var books = entityIds
      .Where(state.Entities.ContainsKey)
      .OrderBy(id => id.Value)
      .Select(state.ProjectedBooks)
      .ToList();

    var transactions = state.Journal
      .Where(transaction => period is null || transaction.Period == period)
      .Where(transaction => Touches(transaction, entityIds))
      .ToList();

    var moneyChange = transactions
      .SelectMany(transaction => transaction.Effects.OfType<PositionChange>())
      .Where(change => change.Asset.Equals(state.MonetaryAssetId))
      .Where(change => entityIds.Contains(EconomicIdentity.ToLegalEntityId(change.Owner)))
      .Sum(change => change.Delta);

    return new FinancialProjection(
      Scope: scope,
      EntityBooks: books,
      TotalAssets: Money.From(books.Sum(book => book.TotalAssets.Amount)),
      TotalLiabilities: Money.From(books.Sum(book => book.TotalLiabilities.Amount)),
      NetWorth: Money.From(books.Sum(book => book.NetWorth.Amount)),
      Cash: Money.From(books.Sum(book => book.Cash.Amount)),
      DepositsHeld: Money.From(books.Sum(book => book.DepositsHeld.Amount)),
      LoansReceivable: Money.From(books.Sum(book => book.LoansReceivable.Amount)),
      LoansPayable: Money.From(books.Sum(book => book.LoansPayable.Amount)),
      ObligationsReceivable: Money.From(books.Sum(book => book.ObligationsReceivable.Amount)),
      ObligationsPayable: Money.From(books.Sum(book => book.ObligationsPayable.Amount)),
      UndrawnCommittedCredit: Money.From(books.Sum(book => book.UndrawnCommittedCredit.Amount)),
      UnpricedHoldings: books.Sum(book => book.HoldingsUnpricedQuantity),
      MonetaryPositionChange: Money.From(moneyChange),
      Transactions: transactions);
  }

  private static bool Touches(
    EconomicTransaction transaction,
    IReadOnlySet<LegalEntityId> entityIds)
  {
    foreach (var change in transaction.Effects.OfType<PositionChange>())
    {
      if (entityIds.Contains(EconomicIdentity.ToLegalEntityId(change.Owner)))
      {
        return true;
      }
    }

    foreach (var claim in transaction.Effects.OfType<CreateClaim>())
    {
      if (entityIds.Contains(EconomicIdentity.ToLegalEntityId(claim.Claim.Creditor))
          || entityIds.Contains(EconomicIdentity.ToLegalEntityId(claim.Claim.Debtor)))
      {
        return true;
      }
    }

    return false;
  }

  private static IReadOnlySet<LegalEntityId> ResolveEntityIds(
    EconomyState state,
    FinancialScope scope)
  {
    return scope switch
    {
      FinancialScope.Entity entity => new HashSet<LegalEntityId> { entity.Id },
      FinancialScope.Cohort cohort => ResolveCohort(state, cohort.Id),
      FinancialScope.Region region => ResolveRegion(state, region.Id),
      FinancialScope.EntityKind kind => state.Entities.Values
        .Where(entity => entity.Kind == kind.Kind)
        .Select(entity => entity.Id)
        .ToHashSet(),
      FinancialScope.Group group => group.Ids,
      _ => throw new ArgumentOutOfRangeException(nameof(scope))
    };
  }

  private static IReadOnlySet<LegalEntityId> ResolveCohort(
    EconomyState state,
    CohortId cohortId)
  {
    if (!state.Cohorts.TryGetValue(cohortId, out var cohort)
        || cohort.HouseholdEntityId is not { } entityId)
    {
      return new HashSet<LegalEntityId>();
    }

    return new HashSet<LegalEntityId> { entityId };
  }

  private static IReadOnlySet<LegalEntityId> ResolveRegion(
    EconomyState state,
    RegionId regionId)
  {
    var ids = state.Cohorts.Values
      .Where(cohort => cohort.RegionId.Equals(regionId))
      .Where(cohort => cohort.HouseholdEntityId is not null)
      .Select(cohort => cohort.HouseholdEntityId!.Value)
      .ToHashSet();

    foreach (var activity in state.Activities.Values.Where(activity => activity.RegionId.Equals(regionId)))
    {
      ids.Add(activity.Operator);
    }

    foreach (var position in state.PositionState.Values.Where(position => position.Region is { } region && region.Equals(regionId)))
    {
      ids.Add(EconomicIdentity.ToLegalEntityId(position.Owner));
    }

    return ids;
  }
}
