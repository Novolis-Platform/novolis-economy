using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Novolis.Economy.Simulation;

/// <summary>Identity metadata for a reproducible simulation run.</summary>
public sealed record SimulationRunManifest(
  string ModelVersion,
  ulong RootSeed,
  ulong InitialStateFingerprint,
  int PeriodHours,
  string LibraryVersion,
  string SpecificationHash,
  string ScenarioHash,
  string ModelId = "LegacyOperational",
  string ModelDescriptorHash = "",
  IReadOnlyList<string>? RuleIdentities = null,
  IReadOnlyList<string>? AgentProfileIds = null)
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
  /// Stable hash of the complete model selection, including rules, actors,
  /// and model-specific scenario inputs.
  /// </summary>
  public static string HashModelDefinition(SimulationModelDefinition model)
  {
    ArgumentNullException.ThrowIfNull(model);
    var payload = new
    {
      model.Identity.Id,
      model.Identity.Version,
      Rules = model.RuleIdentities,
      Agents = model.AgentProfileIds,
      model.Specification,
      Descriptor = model.ReproducibilityDescriptor
    };
    var json = JsonSerializer.Serialize(payload);
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
    string specificationHash,
    string modelId = "LegacyOperational",
    string modelVersion = "legacy",
    string modelDescriptorHash = "")
  {
    ArgumentException.ThrowIfNullOrEmpty(specificationHash);
    ArgumentException.ThrowIfNullOrEmpty(modelId);
    ArgumentException.ThrowIfNullOrEmpty(modelVersion);
    var material =
      $"{modelId}|{modelVersion}|{seed:X16}|{initialStateFingerprint:X16}|{specificationHash}|{modelDescriptorHash}";
    return Convert.ToHexString(
      SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
  }
}
