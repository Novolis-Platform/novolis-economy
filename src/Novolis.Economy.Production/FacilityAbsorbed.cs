using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Facility ownership rebinding after default absorb.</summary>
public sealed record FacilityAbsorbed(
  SimulationHour Hour,
  FacilityId FacilityId,
  FirmId FromFirmId,
  FirmId ToFirmId) : IEconomyEvent;
