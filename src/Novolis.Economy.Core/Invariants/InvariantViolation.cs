namespace Novolis.Economy.Core.Invariants;

/// <summary>Violation report from invariant checks.</summary>
public sealed record InvariantViolation(string Code, string Message);