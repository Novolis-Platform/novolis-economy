using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Immutable snapshot (legacy).</summary>
public sealed record Shipment(
  ShipmentId Id,
  FreightRouteId RouteId,
  ProductId ProductId,
  Quantity Quantity,
  SimulationHour DepartedAt,
  SimulationHour ExpectedArrival,
  ShipmentStatus Status);
