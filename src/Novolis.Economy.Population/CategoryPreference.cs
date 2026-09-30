using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Population;

/// <summary>Relative preference weight for a product category (skeleton).</summary>
/// <param name="CategoryId">Category.</param>
/// <param name="Weight">Relative weight (higher = stronger preference).</param>
public sealed record CategoryPreference(ProductCategoryId CategoryId, decimal Weight);
