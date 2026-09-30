using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Invoice created (AR for seller / AP for buyer).</summary>
public sealed record InvoicePosted(
  SimulationHour Hour,
  Guid InvoiceId,
  FirmId SellerFirmId,
  FirmId? BuyerFirmId,
  Money Amount) : IEconomyEvent;
