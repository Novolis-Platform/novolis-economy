namespace Novolis.Economy.Core;

/// <summary>
/// Aggregate of similar households (SPEC §4).
/// <paramref name="HouseholdEntityId"/> is a Core extension linking the cohort to a Household legal entity for wages/dividends/claims.
/// <paramref name="CashPerHousehold"/> is a legacy cohort-level compatibility
/// projection. Authoritative monetary holdings belong to the linked entity's
/// economic position; this value is not used as a second money stock.
/// </summary>
public sealed record HouseholdCohort(
  CohortId Id,
  RegionId RegionId,
  int HouseholdCount,
  HouseholdProfile Profile,
  HouseholdLaborKind LaborKind,
  Money CashPerHousehold,
  LegalEntityId? HouseholdEntityId = null);