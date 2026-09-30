using System.Security.Cryptography;
using System.Text;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Result of driving a host-neutral model through the generic runner.</summary>
public sealed record EconomicModelRunResult(
    IEconomicModelState State,
    EconomicModelRunManifest Manifest,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions);
