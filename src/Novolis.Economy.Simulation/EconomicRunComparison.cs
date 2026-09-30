using System.Security.Cryptography;
using System.Text;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Range summary for one observation metric across runs.</summary>
public sealed record EconomicRunComparison(
    string Metric,
    IReadOnlyList<EconomicRunMetricValue> Values,
    decimal Minimum,
    decimal Maximum,
    decimal Spread);
