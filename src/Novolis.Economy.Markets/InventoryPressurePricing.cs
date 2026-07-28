using Novolis.Economy;

namespace Novolis.Economy.Markets;

/// <summary>
/// Soft inventory-pressure adjustment for posted prices.
/// High stock → discount; low stock → premium; clamped (toy-but-legible, not a GE solver).
/// </summary>
public static class InventoryPressurePricing
{
  /// <summary>
  /// Adjusts a base posted price from on-hand vs target inventory.
  /// </summary>
  /// <param name="basePrice">Nominal posted unit price.</param>
  /// <param name="onHand">Current stock quantity.</param>
  /// <param name="targetOnHand">Comfort / target stock (must be &gt; 0).</param>
  /// <param name="maxPremium">Max multiplicative premium when empty (e.g. 0.25 = +25%).</param>
  /// <param name="maxDiscount">Max multiplicative discount when overstocked (e.g. 0.25 = −25%).</param>
  public static Money Adjust(
    Money basePrice,
    decimal onHand,
    decimal targetOnHand,
    decimal maxPremium = 0.25m,
    decimal maxDiscount = 0.25m)
  {
    if (basePrice.Amount <= 0m)
    {
      return basePrice;
    }

    if (targetOnHand <= 0m)
    {
      return basePrice;
    }

    // ratio 1 = at target; &lt;1 scarce; &gt;1 abundant
    var ratio = onHand / targetOnHand;
    decimal factor;
    if (ratio >= 1m)
    {
      // Abundant: discount grows toward maxDiscount as ratio → 2+
      var t = Math.Clamp((ratio - 1m) / 1m, 0m, 1m);
      factor = 1m - (maxDiscount * t);
    }
    else
    {
      // Scarce: premium grows toward maxPremium as ratio → 0
      var t = Math.Clamp(1m - ratio, 0m, 1m);
      factor = 1m + (maxPremium * t);
    }

    var adjusted = Math.Round(basePrice.Amount * factor, 4, MidpointRounding.AwayFromZero);
    return Money.From(Math.Max(0.0001m, adjusted));
  }
}
