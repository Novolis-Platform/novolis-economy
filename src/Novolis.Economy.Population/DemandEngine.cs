using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Production;

namespace Novolis.Economy.Population;

/// <summary>Runtime cohort with remaining period budget.</summary>
public sealed class CohortState
{
  /// <summary>Creates cohort state.</summary>
  public CohortState(ConsumerCohort definition)
  {
    Definition = definition;
    BudgetRemaining = definition.DisposableIncome;
  }

  /// <summary>Static definition.</summary>
  public ConsumerCohort Definition { get; }

  /// <summary>Unspent disposable income this period.</summary>
  public Money BudgetRemaining { get; set; }

  /// <summary>Resets budget at period boundary.</summary>
  public void ResetBudget() => BudgetRemaining = Definition.DisposableIncome;
}

/// <summary>Posted-price retail demand clearing.</summary>
public static class DemandEngine
{
  /// <summary>
  /// Each cohort spends remaining budget on preferred products available at retail
  /// locations with posted prices, stock-constrained.
  /// </summary>
  public static void ResolvePurchases(
    IReadOnlyList<CohortState> cohorts,
    IReadOnlyDictionary<(FirmId Firm, FacilityId Facility, ProductId Product), Money> retailPrices,
    IReadOnlyDictionary<FacilityId, (FirmId Firm, InventoryLocationId RetailLocation)> retailFacilities,
    IReadOnlyDictionary<ProductId, ProductDefinition> products,
    InventoryStore inventory,
    IReadOnlyDictionary<FirmId, FirmLedger> ledgers,
    SimulationHour hour,
    Action<IEconomyEvent> emit)
  {
    foreach (var cohort in cohorts.OrderBy(c => c.Definition.Id.Value))
    {
      if (cohort.BudgetRemaining.Amount <= 0m)
      {
        continue;
      }

      var prefs = cohort.Definition.Preferences.CategoryPreferences;
      if (prefs.IsDefaultOrEmpty || prefs.Length == 0)
      {
        continue;
      }

      var weightSum = prefs.Sum(p => p.Weight);
      if (weightSum <= 0m)
      {
        continue;
      }

      foreach (var pref in prefs.OrderByDescending(p => p.Weight).ThenBy(p => p.CategoryId.Value))
      {
        var share = pref.Weight / weightSum;
        var budgetForCategory = Money.From(cohort.BudgetRemaining.Amount * share);
        if (budgetForCategory.Amount <= 0m)
        {
          continue;
        }

        // Find cheapest available retail offer in this category
        var offers =
          from price in retailPrices
          let productId = price.Key.Product
          where products.TryGetValue(productId, out var def) && def.Category.Equals(pref.CategoryId)
          let facility = price.Key.Facility
          where retailFacilities.ContainsKey(facility)
          let retail = retailFacilities[facility]
          let key = new InventoryKey(retail.Firm, retail.RetailLocation, productId)
          let stock = inventory.GetQuantity(key)
          where stock.Value > 0m && price.Value.Amount > 0m
          orderby price.Value.Amount, productId.Value
          select (price.Key.Firm, facility, productId, price.Value, key, stock);

        foreach (var offer in offers)
        {
          if (budgetForCategory.Amount <= 0m || cohort.BudgetRemaining.Amount <= 0m)
          {
            break;
          }

          var maxAffordable = Math.Floor(budgetForCategory.Amount / offer.Value.Amount * 10000m) / 10000m;
          var buyQty = Math.Min(maxAffordable, offer.stock.Value);
          if (buyQty <= 0m)
          {
            continue;
          }

          var qty = Quantity.From(buyQty);
          if (!inventory.TryTake(offer.key, qty, out _, out var cogs))
          {
            continue;
          }

          var revenue = Money.From(offer.Value.Amount * buyQty);
          cohort.BudgetRemaining -= revenue;
          budgetForCategory -= revenue;

          if (ledgers.TryGetValue(offer.Firm, out var ledger))
          {
            LedgerEngine.PostCashSale(ledger, revenue, cogs, hour.Date);
          }

          emit(new GoodsSold(hour, offer.Firm, offer.facility, cohort.Definition.Id, offer.productId, qty, offer.Value, revenue));
          emit(new MarketTradeObserved(hour, offer.productId, qty, offer.Value));
        }
      }
    }
  }
}
