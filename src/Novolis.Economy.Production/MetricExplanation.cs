using System.Collections.Immutable;

namespace Novolis.Economy;

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
