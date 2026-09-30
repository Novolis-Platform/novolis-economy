using Novolis.Economy.Markets;

namespace Novolis.Economy.Simulation;

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
