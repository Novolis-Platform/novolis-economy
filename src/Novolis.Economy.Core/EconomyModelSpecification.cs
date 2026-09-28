namespace Novolis.Economy.Core;

/// <summary>Parameters for the initial posted-price gate model.</summary>
public sealed record PricingSpecification(
    decimal UndercutFactor = 0.97m,
    decimal RisingFactor = 1.02m,
    decimal FallingFactor = 0.94m,
    decimal LowerBandFactor = 0.85m,
    decimal CeilingMultiple = 2.4m);

/// <summary>Parameters for automatic Core credit draws.</summary>
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
/// Serializable declaration of the behavioral assumptions used by a run.
/// These records configure existing formulas; they do not introduce behavior
/// interfaces.
/// </summary>
public sealed record EconomyModelSpecification(
    string Version,
    PricingSpecification Pricing,
    CreditSpecification Credit,
    FiscalSpecification Fiscal,
    DividendSpecification Dividends,
    MigrationSpecification Migration)
{
    /// <summary>Current default model declaration.</summary>
    public static EconomyModelSpecification Default { get; } = new(
        Version: "economy-grammar-1",
        Pricing: new PricingSpecification(),
        Credit: new CreditSpecification(),
        Fiscal: new FiscalSpecification(),
        Dividends: new DividendSpecification(),
        Migration: new MigrationSpecification());
}
