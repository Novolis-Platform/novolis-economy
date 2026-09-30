using System.Text.Json;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Data-only run record. It deliberately omits the opaque model state; a
/// resumable state belongs in an economic snapshot.
/// </summary>
public sealed record EconomicRunRecord(
    string FormatVersion,
    EconomicModelRunManifest Manifest,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions);
