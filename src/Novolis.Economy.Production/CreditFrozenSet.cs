using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Borrower credit frozen after default.</summary>
public sealed record CreditFrozenSet(
  SimulationHour Hour,
  FirmId FirmId) : IEconomyEvent;
