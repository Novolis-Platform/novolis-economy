using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class SettleFinancePhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.SettleFinance;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;

    foreach (var loan in world.Loans.Where(l => l.Status == LoanStatus.Active).OrderBy(l => l.Id.Value))
    {
      CoreClaimBridge.HydrateLoan(world, loan);
      var interest = LoanEngine.AccrueHour(loan, world.Ledgers, hour);
      if (interest.Amount > 0m)
      {
        context.State.AppendEvent(new InterestAccrued(hour, loan.Id, interest));
      }
      CoreClaimBridge.SyncLoan(world, loan);

      if (hour.HourIndex < loan.DueAt.HourIndex)
      {
        continue;
      }

      var owed = loan.PrincipalRemaining;
      var householdLender = world.IsHousehold(loan.LenderFirmId);
      var paid = LoanEngine.TryRepay(
        loan,
        world.Ledgers,
        owed,
        hour,
        householdLender,
        householdLender ? world.CreditHouseholdBudget : null);
      if (paid.Amount > 0m)
      {
        context.State.AppendEvent(new LoanRepaid(hour, loan.Id, paid, loan.PrincipalRemaining));
      }

      if (loan.Status == LoanStatus.Active && loan.PrincipalRemaining.Amount > 0.0000001m)
      {
        loan.Status = LoanStatus.Defaulted;
        context.State.AppendEvent(new LoanDefaulted(
          hour, loan.Id, loan.BorrowerFirmId, loan.PrincipalRemaining));
        DefaultConsequenceEngine.ApplyAbsorb(
          world,
          loan.LenderFirmId,
          loan.BorrowerFirmId,
          hour,
          context.State.AppendEvent);
      }

      CoreClaimBridge.SyncLoan(world, loan);
    }

    return ValueTask.CompletedTask;
  }
}
