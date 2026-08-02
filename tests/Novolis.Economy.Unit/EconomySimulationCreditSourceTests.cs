using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Finance;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit;

public sealed class EconomySimulationCreditSourceTests
{
    [Test]
    public async Task CreditSource_ReflectsLiveSimulationState()
    {
        var treasury = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000005"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000006"));
        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddFirm(treasury, "Treasury", Money.From(20_000m));
        builder.AddFirm(borrower, "Mine", Money.From(500m));
        var sim = new EconomySimulation(31, builder.Build());
        var source = new EconomySimulationCreditSource(sim);

        await Assert.That(source.LiquidStock).IsGreaterThan(0m);
        await Assert.That(source.FirmCash).IsGreaterThan(0m);
        await Assert.That(source.Events.Count).IsEqualTo(0);

        var agent = new TreasuryFirmAgent(treasury, new TreasuryFirmAgentPolicy(
            [borrower], CashFloorToLend: 5_000m, BorrowerCashFloor: 2_000m,
            LoanPrincipal: Money.From(1_000m), AnnualInterestRate: 0.1m, TermHours: 240));
        agent.Tick(new AgentContext(sim, new DeterministicRandom(31)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));

        await Assert.That(source.Events.Count).IsGreaterThan(0);
        await Assert.That(source.Clock.HourIndex).IsEqualTo(1);
        await Assert.That(((ICreditCirculationSource)source).ActiveLoanCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreditCirculation_TracksSimulationEvents()
    {
        var treasury = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000007"));
        var borrower = FirmId.From(Guid.Parse("00000000-0000-4000-8000-000000000008"));
        var builder = new EconomyWorldBuilder(new EconomyPolicy());
        builder.AddFirm(treasury, "Treasury", Money.From(20_000m));
        builder.AddFirm(borrower, "Mine", Money.From(500m));
        var sim = new EconomySimulation(32, builder.Build());
        var source = new EconomySimulationCreditSource(sim);
        var tracker = new CreditCirculation(source);
        var before = source.Events.Count;

        var agent = new TreasuryFirmAgent(treasury, new TreasuryFirmAgentPolicy(
            [borrower], CashFloorToLend: 5_000m, BorrowerCashFloor: 2_000m,
            LoanPrincipal: Money.From(500m), AnnualInterestRate: 0.1m, TermHours: 240));
        agent.Tick(new AgentContext(sim, new DeterministicRandom(32)));
        await sim.AdvanceAsync(SimulationDuration.FromHours(1));
        tracker.ObserveAfterPulse(before);

        await Assert.That(tracker.LoansOriginated).IsEqualTo(1);
        await Assert.That(tracker.MacroLog.Count).IsGreaterThan(0);
    }
}
