namespace Novolis.Economy.Simulation.Models;

/// <summary>Named model compositions shipped by the Simulation package.</summary>
public static class SimulationModels
{
  /// <summary>The documented flagship model for new runs.</summary>
  public static SmallOpenRegionalTradeModel Default =>
    SmallOpenRegionalTrade();

  /// <summary>Flagship small open regional trade model.</summary>
  public static SmallOpenRegionalTradeModel SmallOpenRegionalTrade(
    SmallOpenRegionalTradeSpecification? specification = null) =>
    new(specification);

  /// <summary>Finite deterministic model for tests and teaching.</summary>
  public static DeterministicBoundedModel DeterministicBounded(
    EconomyModelSpecification? specification = null) =>
    new(specification);
}
