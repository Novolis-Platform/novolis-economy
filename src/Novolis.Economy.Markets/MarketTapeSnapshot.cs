using Novolis.Economy;

namespace Novolis.Economy.Markets;

/// <summary>Public read model for one product's observed tape.</summary>
public readonly record struct MarketTapeSnapshot(
  ProductId ProductId,
  Money LastPrice,
  Money PreviousPrice,
  Quantity LastQuantity,
  Quantity CumulativeVolume,
  SimulationHour LastHour,
  int TradeCount);
