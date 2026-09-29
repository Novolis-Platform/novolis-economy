using Novolis.Economy.Markets;

namespace Novolis.Economy.Simulation;

/// <summary>Parameters for automatic credit draws and default timing.</summary>
public sealed record CreditSpecification(
  decimal FacilityInterestRatePerPeriod = 0.01m,
  int FacilityTermPeriods = 4,
  int DelinquencyPeriodsBeforeDefault = 2);

/// <summary>Parameters for simple fiscal behavior.</summary>
public sealed record FiscalSpecification(
  decimal HouseholdTaxRate = 0m,
  decimal FirmTaxRate = 0m);

/// <summary>Parameters for dividend distribution.</summary>
public sealed record DividendSpecification(decimal RetainedCashFloor = 10m);

/// <summary>Parameters for tax-sensitive migration.</summary>
public sealed record MigrationSpecification(
  decimal TaxPushThreshold = 0.28m,
  decimal MinimumMigrationPreference = 0.65m);

/// <summary>
/// Serializable declaration of the behavioral assumptions selected by a
/// Simulation model. Core receives only atomic state transitions.
/// </summary>
public sealed record EconomyModelSpecification(
  string Version,
  PricingSpecification Pricing,
  CreditSpecification Credit,
  FiscalSpecification Fiscal,
  DividendSpecification Dividends,
  MigrationSpecification Migration)
{
  /// <summary>Default model parameters.</summary>
  public static EconomyModelSpecification Default { get; } = new(
    "economy-grammar-1",
    new PricingSpecification(),
    new CreditSpecification(),
    new FiscalSpecification(),
    new DividendSpecification(),
    new MigrationSpecification());
}
