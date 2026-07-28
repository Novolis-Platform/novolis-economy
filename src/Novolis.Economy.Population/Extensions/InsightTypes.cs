namespace Novolis.Economy.Population.Extensions;

/// <summary>Consumer cohort insight.</summary>
public sealed record ConsumerCohortInsight(
    ConsumerCohortId Id,
    GeographicAreaId Area,
    int HouseholdCount,
    Money DisposableIncome,
    decimal LaborHoursPerDay,
    HouseholdProductivityKind Productivity,
    FirmId? HouseholdFirmId);
