namespace Novolis.Economy.Abstractions;

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
