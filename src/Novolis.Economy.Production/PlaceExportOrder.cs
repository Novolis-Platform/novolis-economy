using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Sell inventory into the exogenous export market (infinite demand at the stated unit price floor).</summary>
public sealed record PlaceExportOrder(
  FirmId SellerFirmId,
  InventoryLocationId Origin,
  ProductId ProductId,
  Quantity Quantity,
  Money MinUnitPrice) : IEconomyCommand;
