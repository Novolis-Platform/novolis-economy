namespace Novolis.Economy.Markets;

/// <summary>Parameters for posted-price gate adjustments.</summary>
public sealed record PricingSpecification(
  decimal UndercutFactor = 0.97m,
  decimal RisingFactor = 1.02m,
  decimal FallingFactor = 0.94m,
  decimal LowerBandFactor = 0.85m,
  decimal CeilingMultiple = 2.4m);
