using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Facility upgrade rejected (usually insufficient cash).</summary>
public sealed record FacilityUpgradeFailed(
  SimulationHour Hour,
  FacilityId FacilityId,
  string Reason) : IEconomyEvent;
