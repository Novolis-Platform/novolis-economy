using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Fuel taken from hub inventory onto a shipment.</summary>
public sealed record FuelBunkered(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  ProductId FuelProductId,
  Quantity Quantity) : IEconomyEvent;
