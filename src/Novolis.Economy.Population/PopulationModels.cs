using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Population;

/// <summary>Household count for a cohort (economic resolution; not headcount).</summary>
/// <param name="Value">Number of households.</param>
public readonly record struct PopulationCount(long Value)
{
  /// <inheritdoc />
  public override string ToString() => Value.ToString();
}

/// <summary>Relative preference weight for a product category (skeleton).</summary>
/// <param name="CategoryId">Category.</param>
/// <param name="Weight">Relative weight (higher = stronger preference).</param>
public sealed record CategoryPreference(ProductCategoryId CategoryId, decimal Weight);

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

/// <summary>Aggregated consumer segment (household sector at region resolution).</summary>
/// <param name="Id">Cohort id.</param>
/// <param name="Population">Household count (not headcount).</param>
/// <param name="DisposableIncome">Per-period disposable income stub / opening budget seed.</param>
/// <param name="Preferences">Preference profile.</param>
/// <param name="Area">Home geographic area (habitat/region).</param>
/// <param name="Productivity">Productive hours setting (12/18/24 per household-day).</param>
/// <param name="HouseholdFirmId">Linked <see cref="LegalEntityKind.Household"/> party id.</param>
public sealed record ConsumerCohort(
  ConsumerCohortId Id,
  PopulationCount Population,
  Money DisposableIncome,
  PreferenceProfile Preferences,
  GeographicAreaId Area,
  HouseholdProductivityKind Productivity = HouseholdProductivityKind.Mean,
  FirmId? HouseholdFirmId = null);
