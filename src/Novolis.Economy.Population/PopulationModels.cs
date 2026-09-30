using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Population;

/// <summary>Household count for a cohort (economic resolution; not headcount).</summary>
/// <param name="Value">Number of households.</param>
public readonly record struct PopulationCount(long Value)
{
  /// <inheritdoc />
  public override string ToString() => Value.ToString();
}
