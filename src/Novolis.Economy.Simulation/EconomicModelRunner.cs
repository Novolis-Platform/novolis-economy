using System.Security.Cryptography;
using System.Text;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

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

        return RunFromState(
            request,
            request.Model.CreateState(request.Scenario),
            initialTick: 0);
    }

    /// <summary>
    /// Continues an already restored state without recreating its initial
    /// authority. Commands are keyed by their absolute model tick.
    /// </summary>
    public static EconomicModelRunResult Continue(
        EconomicModelRunRequest request,
        IEconomicModelState initialState)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(initialState);
        if (initialState.Model != request.Model.Identity)
        {
            throw new ArgumentException(
                $"State '{initialState.Model}' does not belong to model " +
                $"'{request.Model.Identity}'.",
                nameof(initialState));
        }

        return RunFromState(request, initialState, initialState.Tick);
    }

    /// <summary>Runs the same request for each supplied seed.</summary>
    public static IReadOnlyList<EconomicModelRunResult> RunSeeds(
        Func<ulong, EconomicModelRunRequest> requestFactory,
        IEnumerable<ulong> seeds)
    {
        ArgumentNullException.ThrowIfNull(requestFactory);
        ArgumentNullException.ThrowIfNull(seeds);
        return seeds.Select(seed => Run(requestFactory(seed))).ToList();
    }

    /// <summary>Runs named variants and retains every seeded result.</summary>
    public static IReadOnlyList<EconomicSensitivityResult> RunVariants(
        IEnumerable<(string VariantId, EconomicModelRunRequest Request)> variants,
        IEnumerable<ulong> seeds)
    {
        ArgumentNullException.ThrowIfNull(variants);
        ArgumentNullException.ThrowIfNull(seeds);
        var seedValues = seeds.ToArray();
        return variants
            .Select(variant => new EconomicSensitivityResult(
                variant.VariantId,
                RunSeeds(
                    seed => variant.Request with { Seed = seed },
                    seedValues)))
            .ToList();
    }

    /// <summary>Compares one observation metric across named run results.</summary>
    public static EconomicRunComparison Compare(
        IEnumerable<(string RunId, EconomicModelRunResult Run)> runs,
        string metric)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentException.ThrowIfNullOrWhiteSpace(metric);
        var values = runs
            .Select(run => new EconomicRunMetricValue(
                run.RunId,
                run.Run.Manifest.Seed,
                run.Run.Observations
                    .Where(observation => observation.Name == metric)
                    .Select(observation => observation.Value)
                    .LastOrDefault()))
            .ToList();
        if (values.Count == 0)
        {
            throw new ArgumentException(
                "At least one run is required.",
                nameof(runs));
        }

        var minimum = values.Min(value => value.Value);
        var maximum = values.Max(value => value.Value);
        return new EconomicRunComparison(
            metric,
            values,
            minimum,
            maximum,
            maximum - minimum);
    }

    private static EconomicModelRunResult RunFromState(
        EconomicModelRunRequest request,
        IEconomicModelState state,
        long initialTick)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Model);
        ArgumentNullException.ThrowIfNull(request.Scenario);
        ArgumentOutOfRangeException.ThrowIfNegative(request.Ticks);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.PeriodLengthTicks);

        var initialFingerprint = state.Fingerprint;
        var observations = new List<EconomicObservation>();
        var transactions = new List<EconomicTransitionReceipt>();

        for (var offset = 1L; offset <= request.Ticks; offset++)
        {
            var tick = checked(initialTick + offset);
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
                request.Model.ActorProfileIds,
                initialTick),
            observations,
            transactions);
    }

    /// <summary>Returns the canonical hash of a model specification.</summary>
    public static string HashSpecification(
        IEconomicModelSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        var json = EconomicJson.SerializeCanonical(
            specification,
            EconomicJson.CreateOptions());
        return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(json)))
            .ToLowerInvariant();
    }
}
