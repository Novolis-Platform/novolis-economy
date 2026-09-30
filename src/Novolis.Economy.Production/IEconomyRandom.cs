using Novolis.Economy.Abstractions;

namespace Novolis.Economy;

/// <summary>Seeded random source for deterministic simulation.</summary>
public interface IEconomyRandom
{
  /// <summary>Current seed state for hashing and diagnostics.</summary>
  ulong State { get; }

  /// <summary>Next uniform double in [0, 1).</summary>
  double NextDouble();

  /// <summary>Next non-negative integer less than <paramref name="maxExclusive"/>.</summary>
  int NextInt(int maxExclusive);
}
