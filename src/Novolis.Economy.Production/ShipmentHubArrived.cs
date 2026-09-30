using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Shipment arrived at a hub (intermediate or final).</summary>
public sealed record ShipmentHubArrived(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  Guid HubId) : IEconomyEvent;
