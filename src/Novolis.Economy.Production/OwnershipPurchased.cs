using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Ownership purchased for cash/budget.</summary>
public sealed record OwnershipPurchased(
  SimulationHour Hour,
  FirmId IssuerFirmId,
  FirmId BuyerFirmId,
  decimal Fraction,
  Money Price) : IEconomyEvent;
