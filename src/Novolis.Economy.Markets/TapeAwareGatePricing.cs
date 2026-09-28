using Novolis.Economy;
using Novolis.Economy.Core;

namespace Novolis.Economy.Markets;

/// <summary>
/// Seed floors/ceilings with <see cref="ObservedMarketBook"/> last price when tape exists.
/// Hosts use the result as a GatePrice for lift bids.
/// </summary>
public static class TapeAwareGatePricing
{
  /// <summary>
  /// Blends last trade (slight undercut; trend nudges) into a floor/ceiling band.
  /// Empty tape returns <paramref name="floor"/>.
  /// </summary>
  public static decimal Gate(
    ObservedMarketBook book,
    ProductId product,
    decimal floor,
    decimal ceilingMultiple = 2.4m)
    => Gate(
      book,
      product,
      floor,
      EconomyModelSpecification.Default.Pricing with { CeilingMultiple = ceilingMultiple });

  /// <summary>Gate pricing using the complete declared model specification.</summary>
  public static decimal Gate(
    ObservedMarketBook book,
    ProductId product,
    decimal floor,
    EconomyModelSpecification specification)
  {
    ArgumentNullException.ThrowIfNull(specification);
    return Gate(book, product, floor, specification.Pricing);
  }

  /// <summary>Gate pricing using declared model parameters.</summary>
  public static decimal Gate(
    ObservedMarketBook book,
    ProductId product,
    decimal floor,
    PricingSpecification pricing)
  {
    ArgumentNullException.ThrowIfNull(pricing);
    var ceiling = floor * pricing.CeilingMultiple;
    if (!book.TryGetTape(product, out var tape) || tape.TradeCount < 1)
    {
      return floor;
    }

    var observed = tape.LastPrice.Amount;
    // Slight undercut of last trade for lift bids; clamp to floor/ceiling band.
    var blended = observed * pricing.UndercutFactor;
    if (book.Trend(product) == MarketTrend.Rising)
    {
      blended = observed * pricing.RisingFactor;
    }
    else if (book.Trend(product) == MarketTrend.Falling)
    {
      blended = observed * pricing.FallingFactor;
    }

    return Math.Clamp(blended, floor * pricing.LowerBandFactor, ceiling);
  }
}
