using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

/// <summary>Restored typed model and state returned by a model codec.</summary>
public sealed record EconomicModelRestore(
    IEconomicModel Model,
    IEconomicScenario Scenario,
    IEconomicModelState State,
    IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>> CommandStream,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions);
