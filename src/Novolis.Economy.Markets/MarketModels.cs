using Novolis.Economy;

namespace Novolis.Economy.Markets;

/// <summary>Named market metric a firm may research.</summary>
public enum MarketMetric
{
  /// <summary>Estimated demand quantity.</summary>
  Demand = 0,
  /// <summary>Estimated supply quantity.</summary>
  Supply = 1,
  /// <summary>Estimated average price.</summary>
  AveragePrice = 2,
  /// <summary>Estimated market share for the querying firm.</summary>
  OwnMarketShare = 3,
}
