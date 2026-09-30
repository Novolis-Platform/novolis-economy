using Novolis.Economy;

namespace Novolis.Economy.Markets;

/// <summary>Skeleton intelligence service that returns empty zero estimates.</summary>
public sealed class NullMarketIntelligenceService : IMarketIntelligenceService
{
  /// <inheritdoc />
  public MarketEstimate Estimate(FirmId firmId, MarketMetric metric, GeographicAreaId area) =>
    new(metric, area, 0m, Percentage.FromPoints(100m), SimulationDate.Epoch);
}
