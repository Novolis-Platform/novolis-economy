using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Accounting;

/// <summary>Debit or credit side of an entry.</summary>
public enum LedgerSide
{
  /// <summary>Debit.</summary>
  Debit = 0,
  /// <summary>Credit.</summary>
  Credit = 1,
}
