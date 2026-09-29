using Novolis.Economy.Abstractions;
using Novolis.Economy.Models.DeterministicBounded;
using Novolis.Economy.Models.SmallOpenRegionalTrade;

namespace Novolis.Economy.Unit.Simulation;

public sealed class ModelContractAcceptanceTests
{
  [Test]
  public async Task SmallOpenRegionalTrade_Declares_Rules_And_Actor_Profiles()
  {
    var model = new SmallOpenRegionalTradeModel();

    await Assert.That(model.Identity.Id).IsEqualTo("SmallOpenRegionalTrade");
    await Assert.That(model.Rules.Count).IsGreaterThanOrEqualTo(6);
    await Assert.That(model.ActorProfileIds)
      .Contains("households.cohort-aggregate");
  }

  [Test]
  public async Task SmallOpenRegionalTrade_One_Tick_Produces_Trade_And_Rationing()
  {
    var model = new SmallOpenRegionalTradeModel();
    var state = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);

    var result = model.Advance(
      state,
      new EconomicTickContext(1, 0, false, [], Seed: 42));
    var next = (SmallOpenRegionalTradeState)result.State;

    await Assert.That(next.ImportedFuel).IsGreaterThan(0m);
    await Assert.That(next.ExportedFood).IsGreaterThan(0m);
    await Assert.That(next.SoldFood).IsGreaterThan(0m);
    await Assert.That(next.UnmetFoodDemand).IsGreaterThan(0m);
    await Assert.That(result.Transactions).IsNotEmpty();
  }

  [Test]
  public async Task SmallOpenRegionalTrade_Closed_Closure_Disables_External_Flows()
  {
    var model = new SmallOpenRegionalTradeModel(
      new SmallOpenRegionalTradeSpecification(
        Closure: MonetaryClosure.Closed));
    var state = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);

    var result = (SmallOpenRegionalTradeState)model.Advance(
      state,
      new EconomicTickContext(1, 0, false, [], Seed: 42)).State;

    await Assert.That(result.ImportedFuel).IsEqualTo(0m);
    await Assert.That(result.ExportedFood).IsEqualTo(0m);
    await Assert.That(result.SoldFood).IsGreaterThan(0m);
  }

  [Test]
  public async Task SmallOpenRegionalTrade_Replays_For_The_Same_Seed()
  {
    var model = new SmallOpenRegionalTradeModel();
    var scenario = SmallOpenRegionalTradeScenario.Baseline;
    var left = model.CreateState(scenario);
    var right = model.CreateState(scenario);

    for (var tick = 1L; tick <= 24; tick++)
    {
      var context = new EconomicTickContext(
        tick,
        (int)((tick - 1) / 24),
        tick % 24 == 0,
        [],
        Seed: 314159);
      left = (SmallOpenRegionalTradeState)model.Advance(left, context).State;
      right = (SmallOpenRegionalTradeState)model.Advance(right, context).State;
    }

    await Assert.That(left.Fingerprint).IsEqualTo(right.Fingerprint);
    await Assert.That(left.CoreState.Journal.Count)
      .IsEqualTo(right.CoreState.Journal.Count);
  }

  [Test]
  public async Task DeterministicBounded_Has_Finite_Stable_Transitions()
  {
    var model = new DeterministicBoundedModel();
    var left = model.CreateState(DeterministicBoundedScenario.Baseline);
    var right = model.CreateState(DeterministicBoundedScenario.Baseline);

    for (var tick = 1L; tick <= 24; tick++)
    {
      var context = new EconomicTickContext(
        tick,
        (int)((tick - 1) / 24),
        tick % 24 == 0,
        [],
        Seed: 1);
      left = (DeterministicBoundedState)model.Advance(left, context).State;
      right = (DeterministicBoundedState)model.Advance(right, context).State;
    }

    await Assert.That(left.Produced).IsGreaterThan(0m);
    await Assert.That(left.Sold).IsGreaterThan(0m);
    await Assert.That(left.Fingerprint).IsEqualTo(right.Fingerprint);
    await Assert.That(left.Authority.Period).IsEqualTo(1);
  }
}
