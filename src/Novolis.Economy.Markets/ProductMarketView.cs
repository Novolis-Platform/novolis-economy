using Novolis.Economy;

namespace Novolis.Economy.Markets;

/// <summary>Projection of product market conditions for UI.</summary>
/// <param name="ProductId">Product.</param>
/// <param name="Demand">Observed or estimated demand.</param>
/// <param name="Supply">Observed or estimated supply.</param>
/// <param name="AveragePrice">Average price.</param>
/// <param name="PlayerMarketShare">Player share.</param>
/// <param name="Trend">Trend label.</param>
public sealed record ProductMarketView(
  ProductId ProductId,
  Quantity Demand,
  Quantity Supply,
  Money AveragePrice,
  Percentage PlayerMarketShare,
  MarketTrend Trend) : IEconomyProjection;
