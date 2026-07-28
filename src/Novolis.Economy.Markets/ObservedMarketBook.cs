using Novolis.Economy;

namespace Novolis.Economy.Markets;

/// <summary>Observed trade tape feeding market estimates.</summary>
public sealed class ObservedMarketBook
{
  private readonly Dictionary<ProductId, MarketTape> _tape = new();

  /// <summary>Products with at least one recorded trade.</summary>
  public IReadOnlyCollection<ProductId> ProductIds => _tape.Keys.ToList();

  /// <summary>Try read observed tape for a product.</summary>
  public bool TryGetTape(ProductId productId, out MarketTapeSnapshot snapshot)
  {
    if (!_tape.TryGetValue(productId, out var tape) || tape.TradeCount == 0)
    {
      snapshot = default;
      return false;
    }

    snapshot = new MarketTapeSnapshot(
      productId,
      tape.LastPrice,
      tape.PreviousPrice,
      tape.LastQuantity,
      tape.CumulativeVolume,
      tape.LastHour,
      tape.TradeCount);
    return true;
  }

  /// <summary>Records a trade.</summary>
  public void RecordTrade(ProductId productId, Quantity quantity, Money unitPrice, SimulationHour hour)
  {
    if (!_tape.TryGetValue(productId, out var tape))
    {
      tape = new MarketTape();
      _tape[productId] = tape;
    }

    // Capture prior last before overwrite so Trend(Rising/Falling) can see a delta.
    if (tape.TradeCount > 0)
    {
      tape.PreviousPrice = tape.LastPrice;
    }
    else
    {
      tape.PreviousPrice = unitPrice;
    }

    tape.LastPrice = unitPrice;
    tape.LastQuantity = quantity;
    tape.CumulativeVolume += quantity;
    tape.LastHour = hour;
    tape.TradeCount++;
  }

  /// <summary>Builds an estimate from observed tape.</summary>
  public MarketEstimate Estimate(ProductId productId, MarketMetric metric, GeographicAreaId area, SimulationDate asOf)
  {
    if (!_tape.TryGetValue(productId, out var tape) || tape.TradeCount == 0)
    {
      return new MarketEstimate(metric, area, 0m, Percentage.FromPoints(100m), asOf);
    }

    var uncertainty = Math.Max(5m, 100m / (1m + tape.TradeCount));
    var point = metric switch
    {
      MarketMetric.AveragePrice => tape.LastPrice.Amount,
      MarketMetric.Demand => tape.CumulativeVolume.Value,
      MarketMetric.Supply => tape.CumulativeVolume.Value,
      _ => tape.LastPrice.Amount,
    };
    return new MarketEstimate(metric, area, point, Percentage.FromPoints(uncertainty), asOf);
  }

  /// <summary>Trend from last two prices.</summary>
  public MarketTrend Trend(ProductId productId)
  {
    if (!_tape.TryGetValue(productId, out var tape) || tape.TradeCount < 2)
    {
      return MarketTrend.Unknown;
    }

    var delta = tape.LastPrice.Amount - tape.PreviousPrice.Amount;
    if (Math.Abs(delta) < 0.0001m)
    {
      return MarketTrend.Stable;
    }

    return delta > 0 ? MarketTrend.Rising : MarketTrend.Falling;
  }

  /// <summary>Intelligence service backed by this book.</summary>
  public IMarketIntelligenceService AsIntelligence(GeographicAreaId defaultArea) =>
    new BookIntelligence(this, defaultArea);

  /// <summary>Fingerprint.</summary>
  public ulong Fingerprint()
  {
    const ulong offset = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offset;
    foreach (var (id, tape) in _tape.OrderBy(kv => kv.Key.Value))
    {
      hash = (hash ^ (ulong)id.Value.GetHashCode()) * prime;
      hash = (hash ^ (ulong)tape.TradeCount) * prime;
      foreach (var b in decimal.GetBits(tape.LastPrice.Amount))
      {
        hash = (hash ^ (ulong)(uint)b) * prime;
      }
    }

    return hash;
  }

  private sealed class MarketTape
  {
    public Money LastPrice { get; set; } = Money.Zero;
    public Money PreviousPrice { get; set; } = Money.Zero;
    public Quantity LastQuantity { get; set; } = Quantity.Zero;
    public Quantity CumulativeVolume { get; set; } = Quantity.Zero;
    public SimulationHour LastHour { get; set; } = SimulationHour.Epoch;
    public int TradeCount { get; set; }
    public bool HasHistory => TradeCount > 0;
  }

  private sealed class BookIntelligence(ObservedMarketBook book, GeographicAreaId area) : IMarketIntelligenceService
  {
    public MarketEstimate Estimate(FirmId firmId, MarketMetric metric, GeographicAreaId requestArea) =>
      book.Estimate(ProductId.From(Guid.Empty), metric, requestArea.Value == Guid.Empty ? area : requestArea, SimulationDate.Epoch);
  }
}

/// <summary>Public read model for one product's observed tape.</summary>
public readonly record struct MarketTapeSnapshot(
  ProductId ProductId,
  Money LastPrice,
  Money PreviousPrice,
  Quantity LastQuantity,
  Quantity CumulativeVolume,
  SimulationHour LastHour,
  int TradeCount);
