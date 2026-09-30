namespace Novolis.Economy.Core;

/// <summary>Lane between regions (SPEC §9).</summary>
public sealed record TransportLane(
  RegionId Origin,
  RegionId Destination,
  int TravelPeriods,
  decimal CapacityPerPeriod);