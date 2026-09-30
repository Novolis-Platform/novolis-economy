using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Runs calibration targets in the existing Simulation layer. It does not
/// assert that an empirical target is true; it reports the comparison.
/// </summary>
public static class EconomicValidationRunner
{
    /// <summary>Executes and evaluates one seeded validation request.</summary>
    public static EconomicValidationReport Validate(
        EconomicValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Model);
        ArgumentNullException.ThrowIfNull(request.Scenario);
        ArgumentNullException.ThrowIfNull(request.Plan);
        ArgumentNullException.ThrowIfNull(request.Plan.Targets);

        var run = EconomicModelRunner.Run(
            new EconomicModelRunRequest(
                request.Model,
                request.Scenario,
                request.Seed,
                request.Ticks,
                request.PeriodLengthTicks,
                request.Commands));

        var finalState = run.State as IEconomicStateValidation;
        var invariants = finalState?.ValidateState()
            ?? Array.Empty<EconomicValidationSignal>();
        var measurements = request.Plan.Targets
            .Select(target => EvaluateTarget(target, run, invariants))
            .ToList();

        return new EconomicValidationReport(
            request.Model.Identity,
            request.Scenario.Id,
            request.Scenario.Version,
            request.Seed,
            request.Ticks,
            EconomicModelRunner.HashSpecification(
                request.Model.Specification),
            invariants,
            measurements);
    }

    private static EconomicValidationMeasurement EvaluateTarget(
        CalibrationTarget target,
        EconomicModelRunResult run,
        IReadOnlyList<EconomicValidationSignal> invariants)
    {
        ArgumentNullException.ThrowIfNull(target);
        var isInvariant = target.Kind == CalibrationTargetKind.InternalInvariant;
        decimal? actual = isInvariant
            ? invariants.All(signal => signal.Passed) ? 1m : 0m
            : Aggregate(
                run.Observations
                    .Where(observation =>
                        string.Equals(
                            observation.Name,
                            target.Metric,
                            StringComparison.Ordinal))
                    .Select(observation => observation.Value)
                    .ToList(),
                target.Aggregation);

        if (actual is null)
        {
            return new EconomicValidationMeasurement(
                target,
                null,
                Passed: false,
                "The run did not emit the requested metric.");
        }

        var passed = IsWithinTarget(target, actual.Value);
        return new EconomicValidationMeasurement(
            target,
            actual,
            passed,
            DescribeComparison(target, actual.Value, passed));
    }

    private static decimal? Aggregate(
        IReadOnlyList<decimal> values,
        CalibrationAggregation aggregation)
    {
        if (values.Count == 0)
            return null;

        return aggregation switch
        {
            CalibrationAggregation.Final => values[^1],
            CalibrationAggregation.Sum => values.Sum(),
            CalibrationAggregation.Mean => values.Average(),
            CalibrationAggregation.Minimum => values.Min(),
            CalibrationAggregation.Maximum => values.Max(),
            _ => throw new ArgumentOutOfRangeException(nameof(aggregation))
        };
    }

    private static bool IsWithinTarget(
        CalibrationTarget target,
        decimal actual)
    {
        if (target.TargetValue is { } expected &&
            Math.Abs(actual - expected) > target.Tolerance)
        {
            return false;
        }

        if (target.Minimum is { } minimum &&
            actual < minimum - target.Tolerance)
        {
            return false;
        }

        if (target.Maximum is { } maximum &&
            actual > maximum + target.Tolerance)
        {
            return false;
        }

        return target.TargetValue is not null ||
            target.Minimum is not null ||
            target.Maximum is not null;
    }

    private static string DescribeComparison(
        CalibrationTarget target,
        decimal actual,
        bool passed)
    {
        var expected = target.TargetValue is { } value
            ? $"target {value}"
            : $"range {target.Minimum?.ToString() ?? "-∞"}.." +
              $"{target.Maximum?.ToString() ?? "+∞"}";
        return $"{(passed ? "passed" : "failed")}: actual {actual} " +
            $"{expected} {target.Unit}; source={target.Source}.";
    }
}
