using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Population;

/// <summary>Preference profile used by purchase choice models (deferred).</summary>
/// <param name="CategoryPreferences">Category weights.</param>
/// <param name="PriceSensitivity">Higher means more price-sensitive.</param>
/// <param name="QualitySensitivity">Higher means more quality-sensitive.</param>
/// <param name="BrandLoyalty">Habit / switching-cost stub.</param>
public sealed record PreferenceProfile(
  ImmutableArray<CategoryPreference> CategoryPreferences,
  decimal PriceSensitivity,
  decimal QualitySensitivity,
  decimal BrandLoyalty);
