using Novolis.Economy.Markets;

namespace Novolis.Economy.Simulation;

/// <summary>Parameters for dividend distribution.</summary>
public sealed record DividendSpecification(decimal RetainedCashFloor = 10m);
