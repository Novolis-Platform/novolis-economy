using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>Simulation policy knobs.</summary>
public sealed class EconomyPolicy
{
  /// <summary>Wage rate per labor hour.</summary>
  public Money WageRatePerHour { get; init; } = Money.From(10m);

  /// <summary>Labor hours required per output unit (default).</summary>
  public decimal LaborHoursPerOutputUnit { get; init; } = 0.1m;

  /// <summary>Accounting period length in hours.</summary>
  public int PeriodHours { get; init; } = SimulationHour.HoursPerDay;

  /// <summary>When true, shelf-life spoilage runs each tick.</summary>
  public bool EnableSpoilage { get; init; }

  /// <summary>Research spend converts to productivity at this rate (per currency unit).</summary>
  public decimal ResearchProductivityPerCurrency { get; init; } = 0.0001m;

  /// <summary>
  /// When true, paid wages increase cohort <c>BudgetRemaining</c> (population-weighted)
  /// so household spending power replaces destroyed firm cash.
  /// Default false preserves legacy wage cash destruction.
  /// </summary>
  public bool HouseholdCreditFromWages { get; init; }

  /// <summary>
  /// Period-close budget policy. Default remints disposable income; use
  /// <see cref="CohortBudgetResetMode.CarryForward"/> for closed-loop money stock.
  /// </summary>
  public CohortBudgetResetMode CohortBudgetResetMode { get; init; } =
    CohortBudgetResetMode.MintFromDisposableIncome;

  /// <summary>
  /// When set, corridor tolls debit the shipper and credit this firm's cash/revenue
  /// (liquid cash conserved). Null keeps legacy “toll burns cash” behavior.
  /// </summary>
  public FirmId? TollBeneficiaryFirmId { get; init; }

  /// <summary>
  /// Retail price elasticity for <see cref="Population.DemandEngine"/>.
  /// 0 = legacy (ignore price vs reference); typical soft values 0.5–1.5.
  /// </summary>
  public decimal PriceElasticity { get; init; }

  /// <summary>Comfort floor per household; invest/lend require budget above floor × count.</summary>
  public Money HouseholdComfortThresholdPerHousehold { get; init; } = Money.From(50m);

  /// <summary>
  /// When true (default if any region exists), firm labor supply comes from region household pools.
  /// </summary>
  public bool UseRegionLaborPools { get; init; } = true;
}
