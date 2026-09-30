using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

/// <summary>
/// Versioned, model-neutral persisted representation of one economic state.
/// Model-specific codecs own the contents of the JSON payloads.
/// </summary>
public sealed record EconomicModelSnapshotDocument(
    string FormatVersion,
    EconomicModelIdentity Model,
    string ScenarioId,
    string ScenarioVersion,
    ulong Seed,
    long Tick,
    int Period,
    ulong StateFingerprint,
    JsonElement Specification,
    JsonElement Scenario,
    JsonElement State,
    IReadOnlyList<EconomicCommandDocument> Commands,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions)
{
    /// <summary>Hash of the canonical typed specification payload.</summary>
    public string? SpecificationHash { get; init; }

    /// <summary>Hash of the canonical model command documents.</summary>
    public string? CommandHash { get; init; }
}
