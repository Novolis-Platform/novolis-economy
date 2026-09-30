using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Production;

/// <summary>Emergent quality score (0–100 skeleton scale).</summary>
/// <param name="Score">Quality points.</param>
public readonly record struct ProductQuality(decimal Score);
