namespace Novolis.Economy.Abstractions;

/// <summary>One state-level validation signal exposed without Core coupling.</summary>
public sealed record EconomicValidationSignal(
    string Code,
    bool Passed,
    string Message);
