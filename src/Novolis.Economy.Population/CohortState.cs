using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Production;

namespace Novolis.Economy.Population;

/// <summary>Runtime cohort with remaining period budget.</summary>
public sealed class CohortState
{
  /// <summary>Creates cohort state.</summary>
  public CohortState(ConsumerCohort definition)
  {
    Definition = definition;
    BudgetRemaining = definition.DisposableIncome;
  }

  /// <summary>Static definition.</summary>
  public ConsumerCohort Definition { get; }

  /// <summary>
  /// Unspent discretionary spending constraint this period. This is not an
  /// authoritative monetary holding; entity positions remain the money stock.
  /// </summary>
  public Money BudgetRemaining { get; set; }

  /// <summary>Resets budget at period boundary.</summary>
  public void ResetBudget() => BudgetRemaining = Definition.DisposableIncome;
}
