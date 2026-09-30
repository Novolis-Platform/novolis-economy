using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>
/// Empty-hull reposition between hubs (no cargo). Still burns fuel, tolls, and drive wear.
/// <paramref name="TransitProfileCode"/> matches <see cref="PlanShipment"/>.
/// </summary>
public sealed record PlanReposition(
  FirmId FirmId,
  Guid OriginHubId,
  Guid DestinationHubId,
  Guid VehicleClassId,
  int TransitProfileCode = 1) : IEconomyCommand;
