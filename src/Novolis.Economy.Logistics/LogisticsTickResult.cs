using Novolis.Economy;
using Novolis.Economy.Production;

namespace Novolis.Economy.Logistics;

/// <summary>Aggregates from one logistics hour.</summary>
public sealed record LogisticsTickResult(
  IReadOnlyList<ActiveShipment> Delivered,
  IReadOnlyDictionary<FirmId, decimal> CrewLaborByFirm,
  Quantity FuelBurned,
  Money FuelBurnValue,
  IReadOnlyDictionary<FirmId, Money> FuelBurnValueByFirm,
  Money TollsPaid,
  Quantity FuelBunkered,
  IReadOnlyList<(ActiveShipment Shipment, TransportCorridorId CorridorId)> LegStarts,
  IReadOnlyList<(ActiveShipment Shipment, TransportHubId HubId)> HubArrivals,
  decimal DriveWear = 0m);
