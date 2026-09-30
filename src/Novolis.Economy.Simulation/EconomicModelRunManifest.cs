using System.Security.Cryptography;
using System.Text;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Reproducibility identity for one generic model run.</summary>
public sealed record EconomicModelRunManifest(
    EconomicModelIdentity Model,
    string ScenarioId,
    string ScenarioVersion,
    ulong Seed,
    long Ticks,
    int PeriodLengthTicks,
    ulong InitialStateFingerprint,
    ulong FinalStateFingerprint,
    string SpecificationHash,
    IReadOnlyList<RuleIdentity> Rules,
    IReadOnlyList<string> ActorProfileIds,
    long InitialTick = 0);
