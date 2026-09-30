namespace Novolis.Economy.Markets.Extensions;

/// <summary>Observed tape for one product.</summary>
public sealed record MarketTapeInsight(
    ProductId ProductId,
    Money LastPrice,
    Quantity CumulativeVolume,
    int TradeCount,
    MarketTrend Trend);
