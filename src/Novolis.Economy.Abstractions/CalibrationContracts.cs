namespace Novolis.Economy.Abstractions;

/// <summary>Distinguishes executable invariants from externally sourced targets.</summary>
public enum CalibrationTargetKind
{
    /// <summary>A property of the implementation that must hold exactly.</summary>
    InternalInvariant = 0,

    /// <summary>An optional value or range sourced outside the implementation.</summary>
    Empirical = 1
}

/// <summary>How repeated observations are reduced to one calibration value.</summary>
public enum CalibrationAggregation
{
    /// <summary>Use the last observation in the run.</summary>
    Final = 0,

    /// <summary>Sum all observations.</summary>
    Sum = 1,

    /// <summary>Use the arithmetic mean.</summary>
    Mean = 2,

    /// <summary>Use the smallest observation.</summary>
    Minimum = 3,

    /// <summary>Use the largest observation.</summary>
    Maximum = 4
}

/// <summary>
/// Serializable target metadata. A target may specify an expected value,
/// an inclusive range, or both a source and description for later review.
/// </summary>
public sealed record CalibrationTarget(
    string Id,
    string Metric,
    string Unit,
    CalibrationTargetKind Kind,
    string ModelVersion,
    string Source,
    decimal? TargetValue = null,
    decimal? Minimum = null,
    decimal? Maximum = null,
    decimal Tolerance = 0m,
    CalibrationAggregation Aggregation = CalibrationAggregation.Final,
    string? Description = null);

/// <summary>Named calibration targets attached to one model specification.</summary>
public sealed record CalibrationPlan(
    string Id,
    string Version,
    IReadOnlyList<CalibrationTarget> Targets);

/// <summary>One state-level validation signal exposed without Core coupling.</summary>
public sealed record EconomicValidationSignal(
    string Code,
    bool Passed,
    string Message);

/// <summary>
/// Optional diagnostic boundary implemented by model states that can expose
/// Core invariant checks without making Abstractions depend on Core.
/// </summary>
public interface IEconomicStateValidation
{
    IReadOnlyList<EconomicValidationSignal> ValidateState();
}
