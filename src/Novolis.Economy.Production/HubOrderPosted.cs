using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Hub order accepted onto the book.</summary>
public sealed record HubOrderPosted(
  SimulationHour Hour,
  Guid OrderId,
  FirmId FirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  HubOrderSide Side,
  Quantity Quantity,
  Money LimitPrice) : IEconomyEvent;
