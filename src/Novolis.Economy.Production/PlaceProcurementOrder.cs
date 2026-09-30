using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Buy from the exogenous input market (infinite supply at the stated unit price ceiling).</summary>
public sealed record PlaceProcurementOrder(
  FirmId BuyerFirmId,
  InventoryLocationId Destination,
  ProductId ProductId,
  Quantity Quantity,
  Money MaxUnitPrice) : IEconomyCommand;
