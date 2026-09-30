using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Shipment entered a corridor leg.</summary>
public sealed record ShipmentLegStarted(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  Guid CorridorId) : IEconomyEvent;
