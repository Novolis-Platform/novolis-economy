using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Shipment left origin.</summary>
public sealed record ShipmentDeparted(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity) : IEconomyEvent;
