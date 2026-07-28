namespace Novolis.Economy.Population.Extensions;

/// <summary>Read-only population / cohort insights.</summary>
public static class ConsumerCohortExtensions
{
    /// <summary>Insight from a cohort definition.</summary>
    public static ConsumerCohortInsight ToInsight(this ConsumerCohort cohort) =>
        new(
            Id: cohort.Id,
            Area: cohort.Area,
            HouseholdCount: HouseholdMath.Count(cohort.Population),
            DisposableIncome: cohort.DisposableIncome,
            LaborHoursPerDay: HouseholdMath.LaborHoursPerDay(cohort.Population, cohort.Productivity),
            Productivity: cohort.Productivity,
            HouseholdFirmId: cohort.HouseholdFirmId);

    /// <summary>Insight from live cohort state (budget as disposable).</summary>
    public static ConsumerCohortInsight ToInsight(this CohortState state) =>
        state.Definition.ToInsight() with { DisposableIncome = state.BudgetRemaining };
}
