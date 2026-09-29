using Novolis.Economy;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Simulation;
using Novolis.Economy.Simulation.Models;

namespace Novolis.Economy.Unit.Simulation;

public sealed class ModelProfileAcceptanceTests
{
  [Test]
  public async Task SmallOpenRegionalTrade_Declares_Composition_And_External_Counterparty()
  {
    var model = SimulationModels.SmallOpenRegionalTrade();
    var simulation = EconomySimulation.FromModel(42, model);

    await Assert.That(simulation.ModelIdentity.Id)
      .IsEqualTo("SmallOpenRegionalTrade");
    await Assert.That(simulation.State.Manifest.ModelId)
      .IsEqualTo("SmallOpenRegionalTrade");
    await Assert.That(simulation.State.World.MonetaryClosure)
      .IsEqualTo(MonetaryClosure.ExternalSector);
    await Assert.That(simulation.State.World.ExternalSectorFirmId)
      .IsNotNull();
    await Assert.That(simulation.State.World.CoreState.CreditFacilities.Count)
      .IsEqualTo(1);
    await Assert.That(model.RuleIdentities.Count)
      .IsGreaterThanOrEqualTo(6);
    await Assert.That(model.AgentProfileIds)
      .Contains("firms.rule-based");
  }

  [Test]
  public async Task SmallOpenRegionalTrade_Settles_Imports_And_Exports_Explicitly()
  {
    var simulation = EconomySimulation.FromModel(
      42,
      SimulationModels.SmallOpenRegionalTrade());

    await simulation.AdvanceAsync(SimulationDuration.OneHour);

    await Assert.That(simulation.State.World.ExternalTrade.ImportsPaid.Amount)
      .IsGreaterThan(0m);
    await Assert.That(simulation.State.World.ExternalTrade.ExportsReceived.Amount)
      .IsGreaterThan(0m);
    await Assert.That(simulation.State.World.ExternalTrade.ImportsByProduct.Count)
      .IsGreaterThan(0);
    await Assert.That(simulation.State.World.ExternalTrade.ExportsByProduct.Count)
      .IsGreaterThan(0);
    await Assert.That(simulation.State.Events.OfType<ProcurementFilled>().Count())
      .IsGreaterThan(0);
    await Assert.That(simulation.State.Events.OfType<ExportFilled>().Count())
      .IsGreaterThan(0);
  }

  [Test]
  public async Task SmallOpenRegionalTrade_Is_Replayable_For_The_Same_Seed()
  {
    var left = EconomySimulation.FromModel(
      314159,
      SimulationModels.SmallOpenRegionalTrade());
    var right = EconomySimulation.FromModel(
      314159,
      SimulationModels.SmallOpenRegionalTrade());

    await left.AdvanceAsync(SimulationDuration.OneDay);
    await right.AdvanceAsync(SimulationDuration.OneDay);

    await Assert.That(left.State.Hash).IsEqualTo(right.State.Hash);
    await Assert.That(left.State.Manifest.ScenarioHash)
      .IsEqualTo(right.State.Manifest.ScenarioHash);
    await Assert.That(left.State.World.ExternalTrade.TradeBalance.Amount)
      .IsEqualTo(right.State.World.ExternalTrade.TradeBalance.Amount);
  }

  [Test]
  public async Task SimulationRunner_Produces_Manifest_And_Comparable_Metrics()
  {
    var result = await SimulationRunner.RunAsync(
      new SimulationRunRequest(
        SimulationModels.DeterministicBounded(),
        9,
        SimulationDuration.OneDay));

    await Assert.That(result.Manifest.ModelId)
      .IsEqualTo("DeterministicBounded");
    await Assert.That(result.Metrics.Get("hours"))
      .IsEqualTo(SimulationDuration.OneDay.Hours);
    await Assert.That(result.Metrics.Get("world-fingerprint"))
      .IsGreaterThan(0m);
  }

  [Test]
  public async Task DeterministicBounded_Has_Exact_Finite_Production()
  {
    var simulation = EconomySimulation.FromModel(
      9,
      SimulationModels.DeterministicBounded());

    await simulation.AdvanceAsync(SimulationDuration.OneDay);

    var firm = LegalEntityId.From(Guid.Parse(
      "b2000000-0000-4000-8000-000000000001"));
    var region = RegionId.From(Guid.Parse(
      "b1000000-0000-4000-8000-000000000001"));
    var product = ResourceId.From(Guid.Parse(
      "b3000000-0000-4000-8000-000000000001"));
    var quantity = HoldingLedger.GetQuantity(
      simulation.State.World.CoreState,
      firm,
      region,
      product);

    await Assert.That(simulation.State.ModelIdentity.Id)
      .IsEqualTo("DeterministicBounded");
    await Assert.That(simulation.State.World.CoreState.Period)
      .IsEqualTo(1);
    await Assert.That(quantity).IsEqualTo(22m);
    await Assert.That(simulation.State.World.CoreState.Flows.ProductionQuantities.Values.Sum())
      .IsEqualTo(2m);
  }

  [Test]
  public async Task DeterministicBounded_Has_Stable_Fingerprint_Independent_Of_Seed()
  {
    var left = EconomySimulation.FromModel(
      1,
      SimulationModels.DeterministicBounded());
    var right = EconomySimulation.FromModel(
      2,
      SimulationModels.DeterministicBounded());

    await Assert.That(left.State.World.Fingerprint())
      .IsEqualTo(right.State.World.Fingerprint());
    await Assert.That(left.State.Manifest.ModelId)
      .IsEqualTo(right.State.Manifest.ModelId);
  }
}
