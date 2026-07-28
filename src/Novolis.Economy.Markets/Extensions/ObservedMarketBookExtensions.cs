namespace Novolis.Economy.Markets.Extensions;

/// <summary>Read-only market book insights.</summary>
public static class ObservedMarketBookExtensions
{
    /// <summary>Tape insight for one product, or null if no tape.</summary>
    public static MarketTapeInsight? ToInsight(this ObservedMarketBook book, ProductId productId)
    {
        if (!book.TryGetTape(productId, out var tape))
            return null;
        return new MarketTapeInsight(
            productId,
            tape.LastPrice,
            tape.CumulativeVolume,
            tape.TradeCount,
            book.Trend(productId));
    }

    /// <summary>Aggregate snapshot across all product tapes.</summary>
    public static MarketBookSnapshot Snapshot(this ObservedMarketBook book)
    {
        var products = new List<MarketTapeInsight>();
        foreach (var id in book.ProductIds.OrderBy(p => p.Value))
        {
            var insight = book.ToInsight(id);
            if (insight is not null)
                products.Add(insight);
        }

        return new MarketBookSnapshot(
            ProductCount: products.Count,
            TotalTrades: products.Sum(p => p.TradeCount),
            Products: products);
    }
}
