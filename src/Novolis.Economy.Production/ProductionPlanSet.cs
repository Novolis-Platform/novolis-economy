using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Production plan accepted.</summary>
public sealed record ProductionPlanSet(
  SimulationHour Hour,
  FirmId FirmId,
  FacilityId FacilityId,
  ProductId ProductId,
  Quantity RatePerHour) : IEconomyEvent;
