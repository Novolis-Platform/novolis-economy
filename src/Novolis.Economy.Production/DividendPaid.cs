using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Dividend cash paid to one owner.</summary>
public sealed record DividendPaid(
  SimulationHour Hour,
  FirmId IssuerFirmId,
  FirmId OwnerFirmId,
  Money Amount) : IEconomyEvent;
