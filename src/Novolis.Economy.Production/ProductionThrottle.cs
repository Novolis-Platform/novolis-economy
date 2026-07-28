namespace Novolis.Economy.Production;

/// <summary>Inventory-pressure production rate helper (throttle when overstocked).</summary>
public static class ProductionThrottle
{
  /// <summary>
  /// Returns a throttled rate: full until ~70% of target, then linear taper to
  /// <paramref name="floorRate"/> at/above target.
  /// </summary>
  public static decimal Rate(
    decimal baseRate,
    decimal onHand,
    decimal targetOnHand,
    decimal floorRate = 0m)
  {
    if (baseRate <= 0m)
    {
      return 0m;
    }

    if (targetOnHand <= 0m)
    {
      return baseRate;
    }

    var startTaper = targetOnHand * 0.7m;
    if (onHand <= startTaper)
    {
      return baseRate;
    }

    if (onHand >= targetOnHand)
    {
      return Math.Max(0m, floorRate);
    }

    var t = (onHand - startTaper) / (targetOnHand - startTaper);
    var rate = baseRate + (floorRate - baseRate) * t;
    return Math.Max(0m, rate);
  }
}
