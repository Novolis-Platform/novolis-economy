using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Corridor toll paid from firm cash.</summary>
public sealed record TransportTollPaid(
  SimulationHour Hour,
  Guid ShipmentId,
  FirmId FirmId,
  Money Amount) : IEconomyEvent;
