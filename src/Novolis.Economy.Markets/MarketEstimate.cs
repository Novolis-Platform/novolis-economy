using Novolis.Economy;

namespace Novolis.Economy.Markets;

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
