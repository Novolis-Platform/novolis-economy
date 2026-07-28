using Novolis.Economy.Population;

namespace Novolis.Economy.Simulation;

/// <summary>Liquid money stock helpers (firm cash + household budgets).</summary>
public static class MoneyStock
{
  /// <summary>Sum of all firm cash balances plus cohort budget remaining.</summary>
  public static decimal Liquid(EconomyWorld world)
  {
    ArgumentNullException.ThrowIfNull(world);
    var firms = world.Ledgers.Values.Sum(l => l.Cash.Amount);
    var households = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    return firms + households;
  }
}
