using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Plan and depart a multi-leg shipment between transport hubs.
/// <paramref name="TransitProfileCode"/> is Logistics TransitProfile ordinal:
/// 0=SlowEconomic, 1=StandardCommercial, 2=PriorityCommercial.
/// </summary>
public sealed record PlanShipment(
  FirmId FirmId,
  Guid OriginHubId,
  Guid DestinationHubId,
  ProductId ProductId,
  Quantity Quantity,
  Guid VehicleClassId,
  int TransitProfileCode = 1) : IEconomyCommand;
