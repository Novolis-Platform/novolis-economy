using Novolis.Economy;
using Novolis.Economy.Simulation;
using TUnit.Core;

namespace Novolis.Economy.Unit.Simulation;

public sealed class PhasePipelineTests
{
  [Test]
  public async Task DefaultPhases_AreInEnumOrder()
  {
    var pipeline = PhasePipeline.CreateDefault();
    var orders = pipeline.Phases.Select(p => p.Order).ToArray();
    var expected = Enum.GetValues<SimulationPhaseOrder>().OrderBy(x => (int)x).ToArray();
    await Assert.That(orders).IsEquivalentTo(expected);
  }

  [Test]
  public async Task AdvanceOneHour_RunsEveryPhaseOnce()
  {
    var sim = new EconomySimulation(seed: 7);
    var result = await sim.AdvanceAsync(SimulationDuration.OneHour);
    await Assert.That(result.HoursAdvanced).IsEqualTo(1);
    await Assert.That(sim.State.LastTickPhases.Count).IsEqualTo(12);
    await Assert.That(result.EventsEmitted).IsEqualTo(12);
    await Assert.That(sim.State.Clock.HourIndex).IsEqualTo(1);
  }

  [Test]
  public async Task IdenticalSeedAndCommands_ProduceIdenticalHash()
  {
    var left = new EconomySimulation(seed: 12345);
    var right = new EconomySimulation(seed: 12345);
    await left.AdvanceAsync(SimulationDuration.FromHours(24));
    await right.AdvanceAsync(SimulationDuration.FromHours(24));
    await Assert.That(left.State.Hash).IsEqualTo(right.State.Hash);
    await Assert.That(left.State.Events.Count).IsEqualTo(right.State.Events.Count);
  }

  [Test]
  public async Task EnqueuedCommand_IsConsumedOnApplyDecisions()
  {
    var sim = new EconomySimulation(seed: 9);
    sim.Enqueue(new SetRetailPrice(FirmId.New(), FacilityId.New(), ProductId.New(), Money.From(4.5m)));
    await Assert.That(sim.State.PendingCommands.Count).IsEqualTo(1);
    await sim.AdvanceAsync(SimulationDuration.OneHour);
    await Assert.That(sim.State.PendingCommands.Count).IsEqualTo(0);
  }
}
