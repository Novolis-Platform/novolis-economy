namespace Novolis.Economy.Core.Extensions;

/// <summary>Insight helpers on household cohorts.</summary>
public static class HouseholdCohortExtensions
{
    /// <summary>Aggregate cash for the cohort.</summary>
    public static Money TotalCash(this HouseholdCohort cohort) =>
        HouseholdMath.TotalCash(cohort);

    /// <summary>Effective labor-hours this period.</summary>
    public static decimal EffectiveLaborHours(this HouseholdCohort cohort) =>
        HouseholdMath.EffectiveLaborHours(cohort);

    /// <summary>Structured insight record.</summary>
    public static CohortInsight ToInsight(this HouseholdCohort cohort) =>
        new(
            Id: cohort.Id,
            RegionId: cohort.RegionId,
            HouseholdCount: cohort.HouseholdCount,
            CashPerHousehold: cohort.CashPerHousehold,
            TotalCash: cohort.TotalCash(),
            EffectiveLaborHours: cohort.EffectiveLaborHours(),
            LaborKind: cohort.LaborKind,
            LaborQuality: cohort.Profile.LaborQuality,
            HouseholdEntityId: cohort.HouseholdEntityId);
}
