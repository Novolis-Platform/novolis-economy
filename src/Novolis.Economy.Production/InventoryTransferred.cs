using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Inventory moved between locations or onto/from a shipment.</summary>
public sealed record InventoryTransferred(
  SimulationHour Hour,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity,
  string Reason) : IEconomyEvent;
