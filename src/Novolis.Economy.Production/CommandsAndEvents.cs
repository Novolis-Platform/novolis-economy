using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>Registers a new firm before subsequent commands use it.</summary>
public sealed record RegisterFirm(
  FirmId FirmId,
  string Name) : IEconomyCommand;
