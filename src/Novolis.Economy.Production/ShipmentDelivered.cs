using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Shipment arrived at destination.</summary>
public sealed record ShipmentDelivered(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  ProductId ProductId,
  Quantity Quantity) : IEconomyEvent;
