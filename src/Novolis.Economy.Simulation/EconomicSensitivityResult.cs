using System.Security.Cryptography;
using System.Text;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>Named model variants evaluated over one or more seeds.</summary>
public sealed record EconomicSensitivityResult(
    string VariantId,
    IReadOnlyList<EconomicModelRunResult> Runs);
