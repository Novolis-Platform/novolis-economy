using Novolis.Economy.Markets;

namespace Novolis.Economy.Simulation;

/// <summary>Parameters for tax-sensitive migration.</summary>
public sealed record MigrationSpecification(
  decimal TaxPushThreshold = 0.28m,
  decimal MinimumMigrationPreference = 0.65m);
