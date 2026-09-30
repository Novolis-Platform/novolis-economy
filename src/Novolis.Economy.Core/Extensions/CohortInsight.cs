namespace Novolis.Economy.Core.Extensions;

/// <summary>Cohort aggregate insight.</summary>
public sealed record CohortInsight(
  CohortId Id,
  RegionId RegionId,
  int HouseholdCount,
  Money CashPerHousehold,
  Money TotalCash,
  decimal EffectiveLaborHours,
  HouseholdLaborKind LaborKind,
  decimal LaborQuality,
  LegalEntityId? HouseholdEntityId);