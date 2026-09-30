using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>One evaluated calibration target.</summary>
public sealed record EconomicValidationMeasurement(
    CalibrationTarget Target,
    decimal? ActualValue,
    bool Passed,
    string Explanation);
