using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Accounting;

/// <summary>Marker that an accounting period should close.</summary>
public sealed record AccountingPeriodClose(
  FirmId FirmId,
  SimulationDate PeriodEnd) : IEconomyCommand;
