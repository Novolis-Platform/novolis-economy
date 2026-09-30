using Novolis.Economy.Markets;

namespace Novolis.Economy.Simulation;

/// <summary>Parameters for simple fiscal behavior.</summary>
public sealed record FiscalSpecification(
  decimal HouseholdTaxRate = 0m,
  decimal FirmTaxRate = 0m);
