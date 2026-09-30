using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Hub order (partially) filled against a counterparty.</summary>
public sealed record HubOrderFilled(
  SimulationHour Hour,
  Guid BuyOrderId,
  Guid SellOrderId,
  FirmId BuyerFirmId,
  FirmId SellerFirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice) : IEconomyEvent;
