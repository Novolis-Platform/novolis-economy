using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Ownership claim changed.</summary>
public sealed record OwnershipChanged(
  SimulationHour Hour,
  FirmId IssuerFirmId,
  FirmId OwnerFirmId,
  decimal Fraction) : IEconomyEvent;
