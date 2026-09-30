namespace Novolis.Economy.Abstractions;

/// <summary>Named calibration targets attached to one model specification.</summary>
public sealed record CalibrationPlan(
    string Id,
    string Version,
    IReadOnlyList<CalibrationTarget> Targets);
