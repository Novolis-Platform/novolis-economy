using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>One contribution in a metric decomposition.</summary>
/// <param name="Label">Human-readable cause label.</param>
/// <param name="Value">Contribution magnitude (same units as the parent metric).</param>
public sealed record MetricContribution(string Label, decimal Value);
