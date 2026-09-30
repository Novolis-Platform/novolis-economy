using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Inter-firm goods sale completed (inventory moved; cash posted).</summary>
public sealed record GoodsSoldInterFirm(
  SimulationHour Hour,
  FirmId SellerFirmId,
  FirmId BuyerFirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice,
  Money Revenue) : IEconomyEvent;
