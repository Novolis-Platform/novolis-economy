using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Request for a generic run of a host-neutral model. The model owns one tick;
/// this type owns only the optional clock and run bookkeeping.
/// </summary>
public sealed record EconomicModelRunRequest(
    IEconomicModel Model,
    IEconomicScenario Scenario,
    ulong Seed,
    long Ticks,
    int PeriodLengthTicks = 24,
    IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>>? Commands = null);

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
    IReadOnlyList<string> ActorProfileIds);

/// <summary>Result of driving a host-neutral model through the generic runner.</summary>
public sealed record EconomicModelRunResult(
    IEconomicModelState State,
    EconomicModelRunManifest Manifest,
    IReadOnlyList<EconomicObservation> Observations,
    IReadOnlyList<EconomicTransitionReceipt> Transactions);

/// <summary>
/// Optional generic executor. Games and other hosts may drive the model
/// directly with their own clock instead.
/// </summary>
public static class EconomicModelRunner
{
    /// <summary>Runs a model for the requested number of ticks.</summary>
    public static EconomicModelRunResult Run(EconomicModelRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Model);
        ArgumentNullException.ThrowIfNull(request.Scenario);
        ArgumentOutOfRangeException.ThrowIfNegative(request.Ticks);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.PeriodLengthTicks);

        var state = request.Model.CreateState(request.Scenario);
        var initialFingerprint = state.Fingerprint;
        var observations = new List<EconomicObservation>();
        var transactions = new List<EconomicTransitionReceipt>();

        for (var tick = 1L; tick <= request.Ticks; tick++)
        {
            var period = checked((int)((tick - 1) / request.PeriodLengthTicks));
            var isPeriodBoundary = tick % request.PeriodLengthTicks == 0;
            var commands = request.Commands?.GetValueOrDefault(tick)
                ?? Array.Empty<IEconomicModelCommand>();
            var result = request.Model.Advance(
                state,
                new EconomicTickContext(
                    tick,
                    period,
                    isPeriodBoundary,
                    commands,
                    Seed: request.Seed));
            state = result.State;
            observations.AddRange(result.Observations);
            transactions.AddRange(result.Transactions);
        }

        return new EconomicModelRunResult(
            state,
            new EconomicModelRunManifest(
                request.Model.Identity,
                request.Scenario.Id,
                request.Scenario.Version,
                request.Seed,
                request.Ticks,
                request.PeriodLengthTicks,
                initialFingerprint,
                state.Fingerprint,
                HashSpecification(request.Model.Specification),
                request.Model.Rules,
                request.Model.ActorProfileIds),
            observations,
            transactions);
    }

    private static string HashSpecification(
        IEconomicModelSpecification specification)
    {
        var json = JsonSerializer.Serialize(
            specification,
            specification.GetType());
        return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(json)))
            .ToLowerInvariant();
    }
}
