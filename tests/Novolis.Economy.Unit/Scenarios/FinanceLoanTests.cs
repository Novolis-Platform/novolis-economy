using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit.Scenarios;

public sealed class FinanceLoanTests
{
  [Test]
  public async Task Originate_Accrue_Repay_ClosesLoan_AndConservesLiquid()
  {
    var lender = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f1"));
    var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f2"));
    var builder = new EconomyWorldBuilder(new EconomyPolicy
    {
      HouseholdCreditFromWages = true,
      CohortBudgetResetMode = CohortBudgetResetMode.CarryForward,
    });
    builder.AddFirm(lender, "Lender", Money.From(10_000m));
    builder.AddFirm(borrower, "Borrower", Money.From(100m));
    var sim = new EconomySimulation(7, builder.Build());
    var openLiquid = MoneyStock.Liquid(sim.State.World);

    sim.Enqueue(new OriginateLoan(lender, borrower, Money.From(1_000m), AnnualInterestRate: 0.365m, TermHours: 48));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    await Assert.That(sim.State.World.Loans.Count).IsEqualTo(1);
    await Assert.That(sim.State.Events.OfType<LoanOriginated>().Count()).IsEqualTo(1);
    await Assert.That(sim.State.World.Ledgers[borrower].Cash.Amount).IsEqualTo(1_100m);

    for (var i = 0; i < 10; i++)
    {
      await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    }

    await Assert.That(sim.State.Events.OfType<InterestAccrued>().Count()).IsGreaterThan(0);
    var loan = sim.State.World.Loans[0];
    await Assert.That(loan.PrincipalRemaining.Amount).IsGreaterThan(1_000m);

    sim.Enqueue(new RepayLoan(loan.Id, Money.From(5_000m)));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    await Assert.That(loan.Status).IsEqualTo(LoanStatus.Closed);
    await Assert.That(MoneyStock.Liquid(sim.State.World)).IsEqualTo(openLiquid);
  }

  [Test]
  public async Task TermDue_WithoutCash_Defaults()
  {
    var lender = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f3"));
    var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-0000000000f4"));
    var builder = new EconomyWorldBuilder(new EconomyPolicy());
    builder.AddFirm(lender, "Lender", Money.From(5_000m));
    builder.AddFirm(borrower, "Borrower", Money.From(0m));
    var sim = new EconomySimulation(9, builder.Build());

    sim.Enqueue(new OriginateLoan(lender, borrower, Money.From(500m), 0.1m, TermHours: 2));
    await sim.AdvanceAsync(SimulationDuration.FromHours(1));
    // Spend borrower cash so term repayment fails.
    var b = sim.State.World.Ledgers[borrower];
    b.Post(AccountRole.WageExpense, AccountRole.Cash, Money.From(500m), sim.State.Clock.Date, "burn");
    await sim.AdvanceAsync(SimulationDuration.FromHours(2));

    await Assert.That(sim.State.Events.OfType<LoanDefaulted>().Count()).IsEqualTo(1);
    await Assert.That(sim.State.World.Loans[0].Status).IsEqualTo(LoanStatus.Defaulted);
  }
}
