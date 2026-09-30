using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

/// <summary>
/// Run material captured alongside a model state snapshot.
/// </summary>
public sealed record EconomicSnapshotCapture(
    ulong Seed,
    long Tick,
    int Period,
    IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>> CommandStream,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions);
