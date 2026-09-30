using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Wages paid from cash.</summary>
public sealed record WagesPaid(
  SimulationHour Hour,
  FirmId FirmId,
  Money Amount) : IEconomyEvent;
