using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>How cohort budgets behave at accounting period close.</summary>
public enum CohortBudgetResetMode
{
  /// <summary>Remint each cohort to its disposable income (legacy open mint).</summary>
  MintFromDisposableIncome = 0,

  /// <summary>Leave <c>BudgetRemaining</c> unchanged (closed-loop credit stock).</summary>
  CarryForward = 1,
}
