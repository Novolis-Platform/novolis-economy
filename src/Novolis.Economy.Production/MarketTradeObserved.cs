using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Observed market trade.</summary>
public sealed record MarketTradeObserved(
  SimulationHour Hour,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice) : IEconomyEvent;
