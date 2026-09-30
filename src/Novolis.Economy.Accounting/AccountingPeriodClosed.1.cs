using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Accounting;

/// <summary>Event that an accounting period closed.</summary>
public sealed record AccountingPeriodClosed(
  FirmId FirmId,
  SimulationDate PeriodEnd,
  SimulationHour ClosedAt) : IEconomyEvent;
