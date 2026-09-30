using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Economy.Abstractions;

/// <summary>
/// Converts one concrete model to and from the stable persistence document.
/// Implementations must identify commands by domain kind, never by CLR name.
/// </summary>
public interface IEconomicModelCodec
{
    /// <summary>Model identity accepted by this codec.</summary>
    EconomicModelIdentity Identity { get; }

    /// <summary>Captures a typed model state into a data-only document.</summary>
    EconomicModelSnapshotDocument Capture(
        IEconomicModel model,
        IEconomicScenario scenario,
        IEconomicModelState state,
        EconomicSnapshotCapture capture);

    /// <summary>Restores and validates a typed model state from a document.</summary>
    EconomicModelRestore Restore(EconomicModelSnapshotDocument document);
}
