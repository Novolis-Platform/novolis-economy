namespace Novolis.Economy.Markets.Extensions;

/// <summary>Observed tape for one product.</summary>
public sealed record MarketTapeInsight(
    ProductId ProductId,
    Money LastPrice,
    Quantity CumulativeVolume,
    int TradeCount,
    MarketTrend Trend);

/// <summary>Market book snapshot.</summary>
public sealed record MarketBookSnapshot(
    int ProductCount,
    int TotalTrades,
    IReadOnlyList<MarketTapeInsight> Products);
