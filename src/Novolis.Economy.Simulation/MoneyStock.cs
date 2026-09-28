using Novolis.Economy.Population;
using Novolis.Economy.Core.Extensions;

namespace Novolis.Economy.Simulation;

/// <summary>Operational spending-constraint helpers.</summary>
public static class MoneyStock
{
  /// <summary>
  /// Sum of operational firm cash and household spending constraints. This is
  /// retained for compatibility with hourly ops reports and is not the
  /// authoritative economic money stock.
  /// </summary>
  public static decimal Liquid(EconomyWorld world)
  {
    ArgumentNullException.ThrowIfNull(world);
    var firms = world.Ledgers.Values.Sum(l => l.Cash.Amount);
    var households = world.Cohorts.Sum(c => c.BudgetRemaining.Amount);
    return firms + households;
  }

  /// <summary>Authoritative Core monetary position total.</summary>
  public static decimal MonetaryWealth(EconomyWorld world) =>
    world.CoreState.TotalCash().Amount;
}
