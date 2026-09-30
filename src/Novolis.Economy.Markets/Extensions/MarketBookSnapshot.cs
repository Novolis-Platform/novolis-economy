namespace Novolis.Economy.Markets.Extensions;

/// <summary>Market book snapshot.</summary>
public sealed record MarketBookSnapshot(
    int ProductCount,
    int TotalTrades,
    IReadOnlyList<MarketTapeInsight> Products);
