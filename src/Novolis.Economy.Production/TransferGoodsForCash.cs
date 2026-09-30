using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Sell inventory from one firm to another for cash at a shared location.</summary>
public sealed record TransferGoodsForCash(
  FirmId SellerFirmId,
  FirmId BuyerFirmId,
  InventoryLocationId LocationId,
  ProductId ProductId,
  Quantity Quantity,
  Money UnitPrice) : IEconomyCommand;
