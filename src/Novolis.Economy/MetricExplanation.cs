using System.Collections.Immutable;

namespace Novolis.Economy;

/// <summary>One contribution in a metric decomposition.</summary>
/// <param name="Label">Human-readable cause label.</param>
/// <param name="Value">Contribution magnitude (same units as the parent metric).</param>
public sealed record MetricContribution(string Label, decimal Value);

/// <summary>Explainable metric with a summary and ordered contributions.</summary>
/// <param name="Summary">Short human summary.</param>
/// <param name="Value">Aggregate metric value.</param>
/// <param name="Contributions">Decomposition parts.</param>
public sealed record MetricExplanation(
  string Summary,
  decimal Value,
  ImmutableArray<MetricContribution> Contributions)
{
  /// <summary>Creates an explanation with no contributions.</summary>
  public static MetricExplanation Empty(string summary, decimal value) =>
    new(summary, value, ImmutableArray<MetricContribution>.Empty);
}
