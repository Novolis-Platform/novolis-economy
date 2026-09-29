using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class CloseAccountingPeriodPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.CloseAccountingPeriod;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;
    var period = world.Policy.PeriodHours;
    if (period <= 0 || (hour.HourIndex + 1) % period != 0)
    {
      return ValueTask.CompletedTask;
    }

    // Simulation selects and runs the period model; Core remains the authority
    // for the resulting atomic state transition.
    world.CoreState = context.PeriodEngine.Advance(world.CoreState).Economy;

    foreach (var firmId in world.Firms.Keys.OrderBy(id => id.Value))
    {
      context.State.AppendEvent(new AccountingPeriodClosed(firmId, hour.Date, hour));
    }

    if (world.Policy.CohortBudgetResetMode == CohortBudgetResetMode.CarryForward)
    {
      return ValueTask.CompletedTask;
    }

    foreach (var cohort in world.Cohorts)
    {
      cohort.ResetBudget();
    }

    return ValueTask.CompletedTask;
  }
}
