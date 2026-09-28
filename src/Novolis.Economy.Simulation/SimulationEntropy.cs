using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Novolis.Economy;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Root-seeded deterministic entropy with isolated named streams.
/// </summary>
public sealed class SimulationEntropy
{
  private readonly Dictionary<string, DeterministicRandom> _streams = new(StringComparer.Ordinal);

  /// <summary>Creates entropy for one simulation run.</summary>
  public SimulationEntropy(ulong rootSeed)
  {
    RootSeed = rootSeed;
  }

  /// <summary>Root seed recorded in run metadata.</summary>
  public ulong RootSeed { get; }

  /// <summary>
  /// Gets a persistent deterministic stream for a stable key such as
  /// <c>firms/{id}</c> or <c>markets</c>.
  /// </summary>
  public DeterministicRandom Stream(string key)
  {
    ArgumentException.ThrowIfNullOrEmpty(key);
    if (_streams.TryGetValue(key, out var stream))
      return stream;

    stream = new DeterministicRandom(DeriveSeed(RootSeed, key));
    _streams[key] = stream;
    return stream;
  }

  /// <summary>Derives a deterministic Guid for a named stream sequence.</summary>
  public Guid GuidFor(string key, long sequence)
  {
    ArgumentException.ThrowIfNullOrEmpty(key);
    var digest = SHA256.HashData(
      Encoding.UTF8.GetBytes($"{RootSeed}|{key}|{sequence}"));
    return new Guid(digest.AsSpan(0, 16));
  }

  private static ulong DeriveSeed(ulong rootSeed, string key)
  {
    var digest = SHA256.HashData(
      Encoding.UTF8.GetBytes($"{rootSeed}|{key}"));
    return BinaryPrimitives.ReadUInt64LittleEndian(digest.AsSpan(0, sizeof(ulong)));
  }
}
