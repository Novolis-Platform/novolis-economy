using Novolis.Economy;

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
  {
    var ceiling = floor * ceilingMultiple;
    if (!book.TryGetTape(product, out var tape) || tape.TradeCount < 1)
    {
      return floor;
    }

    var observed = tape.LastPrice.Amount;
    // Slight undercut of last trade for lift bids; clamp to floor/ceiling band.
    var blended = observed * 0.97m;
    if (book.Trend(product) == MarketTrend.Rising)
    {
      blended = observed * 1.02m;
    }
    else if (book.Trend(product) == MarketTrend.Falling)
    {
      blended = observed * 0.94m;
    }

    return Math.Clamp(blended, floor * 0.85m, ceiling);
  }
}
