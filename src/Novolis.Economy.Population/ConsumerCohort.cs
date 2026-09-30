using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Population;

/// <summary>Aggregated consumer segment (household sector at region resolution).</summary>
/// <param name="Id">Cohort id.</param>
/// <param name="Population">Household count (not headcount).</param>
/// <param name="DisposableIncome">Per-period disposable income stub / opening budget seed.</param>
/// <param name="Preferences">Preference profile.</param>
/// <param name="Area">Home geographic area (habitat/region).</param>
/// <param name="Productivity">Productive hours setting (12/18/24 per household-day).</param>
/// <param name="HouseholdFirmId">Linked household legal-entity party id.</param>
public sealed record ConsumerCohort(
  ConsumerCohortId Id,
  PopulationCount Population,
  Money DisposableIncome,
  PreferenceProfile Preferences,
  GeographicAreaId Area,
  HouseholdProductivityKind Productivity = HouseholdProductivityKind.Mean,
  FirmId? HouseholdFirmId = null);
