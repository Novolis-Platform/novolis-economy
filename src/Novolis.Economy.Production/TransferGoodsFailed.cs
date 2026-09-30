using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Inter-firm goods transfer could not complete.</summary>
public sealed record TransferGoodsFailed(
  SimulationHour Hour,
  FirmId SellerFirmId,
  FirmId BuyerFirmId,
  ProductId ProductId,
  string Reason) : IEconomyEvent;
