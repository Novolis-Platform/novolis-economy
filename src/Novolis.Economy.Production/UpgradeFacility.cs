using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Spend cash to scale facility manufacturing/assembly capacity.</summary>
public sealed record UpgradeFacility(
  FacilityId FacilityId,
  Money Cost,
  decimal CapacityFactor) : IEconomyCommand;
