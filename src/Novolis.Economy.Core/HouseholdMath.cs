namespace Novolis.Economy.Core;

/// <summary>Cohort aggregate helpers (SPEC §4).</summary>
public static class HouseholdMath
{
    /// <summary>Total cash held by the cohort (count × cash per household).</summary>
    public static Money TotalCash(HouseholdCohort cohort) =>
        Money.From(cohort.CashPerHousehold.Amount * cohort.HouseholdCount);

    /// <summary>Effective labor-hours available from the cohort this period (SPEC §5).</summary>
    public static decimal EffectiveLaborHours(HouseholdCohort cohort) =>
        cohort.HouseholdCount
        * HouseholdLabor.HoursPerDay(cohort.LaborKind)
        * cohort.Profile.LaborQuality;
}
