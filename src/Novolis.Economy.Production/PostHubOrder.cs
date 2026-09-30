using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Post a limit order at a hub inventory location.</summary>
public sealed record PostHubOrder(
  FirmId FirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  HubOrderSide Side,
  Quantity Quantity,
  Money LimitPrice) : IEconomyCommand;
