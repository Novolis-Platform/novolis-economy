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

/// <summary>Deterministic xorshift64* PRNG.</summary>
public sealed class DeterministicRandom : IEconomyRandom
{
  private ulong _state;

  /// <summary>Creates a PRNG from a non-zero seed.</summary>
  public DeterministicRandom(ulong seed)
  {
    _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
  }

  /// <inheritdoc />
  public ulong State => _state;

  /// <inheritdoc />
  public double NextDouble()
  {
    // Unit interval from top 53 bits.
    return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
  }

  /// <inheritdoc />
  public int NextInt(int maxExclusive)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
    return (int)(NextUInt64() % (ulong)maxExclusive);
  }

  private ulong NextUInt64()
  {
    var x = _state;
    x ^= x >> 12;
    x ^= x << 25;
    x ^= x >> 27;
    _state = x;
    return unchecked(x * 0x2545F4914F6CDD1DUL);
  }
}
