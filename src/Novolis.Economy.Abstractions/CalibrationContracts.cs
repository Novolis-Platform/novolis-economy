namespace Novolis.Economy.Abstractions;

/// <summary>Distinguishes executable invariants from externally sourced targets.</summary>
public enum CalibrationTargetKind
{
    /// <summary>A property of the implementation that must hold exactly.</summary>
    InternalInvariant = 0,

    /// <summary>An optional value or range sourced outside the implementation.</summary>
    Empirical = 1
}
