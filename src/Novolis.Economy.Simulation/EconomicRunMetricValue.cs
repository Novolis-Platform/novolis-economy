using System.Security.Cryptography;
using System.Text;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>One named observation value used in run comparisons.</summary>
public sealed record EconomicRunMetricValue(
    string RunId,
    ulong Seed,
    decimal Value);
