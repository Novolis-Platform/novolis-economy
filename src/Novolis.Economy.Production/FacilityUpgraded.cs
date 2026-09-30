using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Facility capacity increased after cash investment.</summary>
public sealed record FacilityUpgraded(
  SimulationHour Hour,
  FacilityId FacilityId,
  FirmId OwnerFirmId,
  Money Cost,
  decimal CapacityFactor,
  Quantity ManufacturingCapacity) : IEconomyEvent;
