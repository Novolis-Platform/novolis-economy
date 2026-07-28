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

/// <summary>Directional market trend label (skeleton).</summary>
public enum MarketTrend
{
  /// <summary>Insufficient data.</summary>
  Unknown = 0,
  /// <summary>Rising.</summary>
  Rising = 1,
  /// <summary>Stable.</summary>
  Stable = 2,
  /// <summary>Falling.</summary>
  Falling = 3,
}

/// <summary>Imperfect estimate returned to a firm.</summary>
/// <param name="Metric">Metric estimated.</param>
/// <param name="Area">Geographic scope.</param>
/// <param name="PointEstimate">Central estimate (metric-specific units).</param>
/// <param name="Uncertainty">Relative uncertainty (higher = less confident).</param>
/// <param name="AsOf">Estimate vintage.</param>
public sealed record MarketEstimate(
  MarketMetric Metric,
  GeographicAreaId Area,
  decimal PointEstimate,
  Percentage Uncertainty,
  SimulationDate AsOf);

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

/// <summary>Provides imperfect market estimates to firms.</summary>
public interface IMarketIntelligenceService
{
  /// <summary>Estimates a metric for a firm in an area.</summary>
  MarketEstimate Estimate(FirmId firmId, MarketMetric metric, GeographicAreaId area);
}

/// <summary>Skeleton intelligence service that returns empty zero estimates.</summary>
public sealed class NullMarketIntelligenceService : IMarketIntelligenceService
{
  /// <inheritdoc />
  public MarketEstimate Estimate(FirmId firmId, MarketMetric metric, GeographicAreaId area) =>
    new(metric, area, 0m, Percentage.FromPoints(100m), SimulationDate.Epoch);
}
