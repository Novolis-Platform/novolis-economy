using Novolis.Economy;

namespace Novolis.Economy.Markets;

/// <summary>Provides imperfect market estimates to firms.</summary>
public interface IMarketIntelligenceService
{
  /// <summary>Estimates a metric for a firm in an area.</summary>
  MarketEstimate Estimate(FirmId firmId, MarketMetric metric, GeographicAreaId area);
}
