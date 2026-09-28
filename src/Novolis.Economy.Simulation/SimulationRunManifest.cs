using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Novolis.Economy.Core;

namespace Novolis.Economy.Simulation;

/// <summary>Identity metadata for a reproducible simulation run.</summary>
public sealed record SimulationRunManifest(
  string ModelVersion,
  ulong RootSeed,
  ulong InitialStateFingerprint,
  int PeriodHours,
  string LibraryVersion,
  string SpecificationHash,
  string ScenarioHash)
{
  /// <summary>Stable hash of the serialized behavioral specification.</summary>
  public static string HashSpecification(EconomyModelSpecification specification)
  {
    ArgumentNullException.ThrowIfNull(specification);
    var json = JsonSerializer.Serialize(specification);
    return Convert.ToHexString(
      SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
  }

  /// <summary>
  /// Stable identity for the initial scenario, including its declared model
  /// specification and seed.
  /// </summary>
  public static string HashScenario(
    ulong seed,
    ulong initialStateFingerprint,
    string specificationHash)
  {
    ArgumentException.ThrowIfNullOrEmpty(specificationHash);
    var material = $"{seed:X16}|{initialStateFingerprint:X16}|{specificationHash}";
    return Convert.ToHexString(
      SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
  }
}
