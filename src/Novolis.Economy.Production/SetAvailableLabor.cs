using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Set available labor hours per firm per tick.</summary>
public sealed record SetAvailableLabor(
  FirmId FirmId,
  decimal HoursPerTick) : IEconomyCommand;
