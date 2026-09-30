namespace Novolis.Economy.Abstractions;

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
