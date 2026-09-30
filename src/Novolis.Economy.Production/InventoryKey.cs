using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Inventory keyed by firm, location, and product.</summary>
public readonly record struct InventoryKey(
  FirmId FirmId,
  InventoryLocationId LocationId,
  ProductId ProductId);
