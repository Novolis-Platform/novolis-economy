using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Accounting;

/// <summary>Single ledger line.</summary>
public sealed record LedgerEntry(
  Guid EntryId,
  AccountId AccountId,
  FirmId FirmId,
  LedgerSide Side,
  Money Amount,
  SimulationDate Date,
  string? Memo);
