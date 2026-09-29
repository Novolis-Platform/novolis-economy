using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Explicit treatment of monetary flows that do not close domestically.</summary>
public enum MonetaryClosure
{
    Open = 0,
    Closed = 1,
    ExternalSector = 2
}

/// <summary>Stable identity of an economic model, independent of one run.</summary>
public readonly record struct EconomicModelIdentity(
    string Id,
    string Version)
{
    public override string ToString() => $"{Id}@{Version}";
}

/// <summary>Serializable structural and institutional assumptions of a model.</summary>
public interface IEconomicModelSpecification
{
    string Version { get; }
}

/// <summary>Named initial conditions and calibration for one model.</summary>
public interface IEconomicScenario
{
    string Id { get; }

    string Version { get; }
}

/// <summary>Opaque state owned by a concrete economic model.</summary>
public interface IEconomicModelState
{
    EconomicModelIdentity Model { get; }

    long Tick { get; }

    ulong Fingerprint { get; }
}

/// <summary>Action supplied by a game, actor host, or research harness.</summary>
public interface IEconomicModelCommand
{
    string Kind { get; }
}

/// <summary>One model-owned transition request for the authoritative kernel.</summary>
public sealed record EconomicEffectRequest(
    EconomicEntityId? Actor,
    string Kind,
    EconomicEntityId? Owner = null,
    EconomicAssetId? Asset = null,
    decimal Quantity = 0m,
    RegionId? Region = null,
    string? Reason = null);

/// <summary>Read-side receipt for an authoritative economic transition.</summary>
public sealed record EconomicTransitionReceipt(
    TransactionId TransactionId,
    string Reason,
    int Period,
    long Tick,
    IReadOnlyList<EconomicEffectRequest> Effects);

/// <summary>Context for one host-driven model tick.</summary>
public sealed record EconomicTickContext(
    long Tick,
    int Period,
    bool IsPeriodBoundary,
    IReadOnlyList<IEconomicModelCommand> Commands,
    IAgentRandom? Random = null,
    ulong Seed = 0);

/// <summary>Small, unit-labelled scalar observation emitted by a model tick.</summary>
public sealed record EconomicObservation(
    string Name,
    decimal Value,
    string Unit,
    string? Description = null);

/// <summary>Result of one host-driven model transition.</summary>
public sealed record EconomicTickResult(
    IEconomicModelState State,
    IReadOnlyList<EconomicTransitionReceipt> Transactions,
    IReadOnlyList<EconomicObservation> Observations);

/// <summary>
/// Host-neutral economic model. A model describes rules and applies one tick;
/// it does not own a clock, run lifecycle, scheduler, or product host.
/// </summary>
public interface IEconomicModel
{
    EconomicModelIdentity Identity { get; }

    IEconomicModelSpecification Specification { get; }

    IReadOnlyList<RuleIdentity> Rules { get; }

    IReadOnlyList<string> ActorProfileIds { get; }

    IEconomicModelState CreateState(IEconomicScenario scenario);

    EconomicTickResult Advance(
        IEconomicModelState state,
        EconomicTickContext context);
}
