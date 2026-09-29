using Novolis.Economy.Abstractions;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Models.SmallOpenRegionalTrade;

namespace Novolis.Economy.Unit.Simulation;

public sealed class ModelUseCaseAcceptanceTests
{
  [Test]
  public async Task ImportReliefIsVisibleAsPhysicalAndFinancialChange()
  {
    var openModel = new SmallOpenRegionalTradeModel();
    var closedTradeModel = new SmallOpenRegionalTradeModel(
      new SmallOpenRegionalTradeSpecification(
        ImportCapacityPerTick: 0m,
        ExportDemandPerTick: 0m));

    var open = (SmallOpenRegionalTradeState)openModel.Advance(
      openModel.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(1, 0, false, [], Seed: 101)).State;
    var closed = (SmallOpenRegionalTradeState)closedTradeModel.Advance(
      closedTradeModel.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(1, 0, false, [], Seed: 101)).State;

    await Assert.That(open.ImportedFuel).IsGreaterThan(closed.ImportedFuel);
    await Assert.That(open.CoreState.Journal.Count)
      .IsGreaterThan(closed.CoreState.Journal.Count);
  }

  [Test]
  public async Task SupplyShockReducesOutputAndIncreasesUnmetDemand()
  {
    var normalModel = new SmallOpenRegionalTradeModel();
    var shockModel = new SmallOpenRegionalTradeModel(
      new SmallOpenRegionalTradeSpecification(
        FoodProductionCapacityPerTick: 0m));

    var normal = (SmallOpenRegionalTradeState)normalModel.Advance(
      normalModel.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(1, 0, false, [], Seed: 102)).State;
    var shock = (SmallOpenRegionalTradeState)shockModel.Advance(
      shockModel.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(1, 0, false, [], Seed: 102)).State;

    await Assert.That(normal.ProducedFood).IsGreaterThan(shock.ProducedFood);
    await Assert.That(shock.UnmetFoodDemand).IsGreaterThan(normal.UnmetFoodDemand);
  }

  [Test]
  public async Task InvalidActorCommandIsRejectedBeforeStateMutation()
  {
    var model = new SmallOpenRegionalTradeModel();
    var state = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);
    var before = state.Fingerprint;

    var act = () => model.Advance(
      state,
      new EconomicTickContext(
        1,
        0,
        false,
        [new PurchaseFoodCommand(-1m)],
        Seed: 103));

    await Assert.That(act).Throws<ArgumentOutOfRangeException>();
    await Assert.That(state.Fingerprint).IsEqualTo(before);
  }

  [Test]
  public async Task WorkingCapitalCommandCreatesAndAccruesAnAuthoritativeClaim()
  {
    var model = new SmallOpenRegionalTradeModel();
    var state = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);

    state = (SmallOpenRegionalTradeState)model.Advance(
      state,
      new EconomicTickContext(
        1,
        0,
        false,
        [new DrawWorkingCapitalCommand(100m)],
        Seed: 105)).State;

    var claimAfterOrigination = ClaimLedger.Snapshot(state.CoreState)
      .Values.Single();
    await Assert.That(claimAfterOrigination.Principal.Quantity)
      .IsEqualTo(100m);

    for (var tick = 2L; tick <= 24; tick++)
    {
      state = (SmallOpenRegionalTradeState)model.Advance(
        state,
        new EconomicTickContext(
          tick,
          0,
          tick == 24,
          [],
          Seed: 105)).State;
    }

    var claimAfterAccrual = ClaimLedger.Snapshot(state.CoreState)
      .Values.Single();
    await Assert.That(claimAfterAccrual.Principal.Quantity)
      .IsGreaterThan(claimAfterOrigination.Principal.Quantity);
  }

  [Test]
  public async Task ClosedAndZeroExternalFlowRunsHaveTheSameLocalOutcome()
  {
    var closedModel = new SmallOpenRegionalTradeModel(
      new SmallOpenRegionalTradeSpecification(
        Closure: MonetaryClosure.Closed));
    var zeroFlowModel = new SmallOpenRegionalTradeModel(
      new SmallOpenRegionalTradeSpecification(
        ImportCapacityPerTick: 0m,
        ExportDemandPerTick: 0m));

    var closed = (SmallOpenRegionalTradeState)closedModel.Advance(
      closedModel.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(1, 0, false, [], Seed: 104)).State;
    var zeroFlow = (SmallOpenRegionalTradeState)zeroFlowModel.Advance(
      zeroFlowModel.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(1, 0, false, [], Seed: 104)).State;

    await Assert.That(closed.SoldFood).IsEqualTo(zeroFlow.SoldFood);
    await Assert.That(closed.ProducedFood).IsEqualTo(zeroFlow.ProducedFood);
    await Assert.That(closed.UnmetFoodDemand).IsEqualTo(zeroFlow.UnmetFoodDemand);
  }

  [Test]
  public async Task PostedPriceBaselineKeepsItsDeclaredPrice()
  {
    var model = new SmallOpenRegionalTradeModel();
    var result = model.Advance(
      model.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(1, 0, false, [], Seed: 106));

    await Assert.That(result.Observations.Single(item => item.Name == "food-price").Value)
      .IsEqualTo(10m);
    await Assert.That(model.Rules)
      .Contains(rule => rule.Name == "posted-price-rationing");
  }

  [Test]
  public async Task PriceDiscoveryRespondsToDeclaredSupplyPressure()
  {
    var abundantModel = new SmallOpenRegionalTradeModel(
      new SmallOpenRegionalTradeSpecification(
        MarketMechanism: FoodMarketMechanism.SupplyDemandPriceDiscovery,
        FoodProductionCapacityPerTick: 200m));
    var scarceModel = new SmallOpenRegionalTradeModel(
      new SmallOpenRegionalTradeSpecification(
        MarketMechanism: FoodMarketMechanism.SupplyDemandPriceDiscovery));

    var abundant = abundantModel.Advance(
      abundantModel.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(1, 0, false, [], Seed: 107));
    var scarce = scarceModel.Advance(
      scarceModel.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(1, 0, false, [], Seed: 107));
    var abundantPrice = abundant.Observations
      .Single(item => item.Name == "food-price").Value;
    var scarcePrice = scarce.Observations
      .Single(item => item.Name == "food-price").Value;

    await Assert.That(abundantPrice).IsLessThan(10m);
    await Assert.That(scarcePrice).IsGreaterThan(abundantPrice);
    await Assert.That(abundantModel.Rules)
      .Contains(rule => rule.Name == "supply-demand-price-discovery");
  }

  [Test]
  public async Task LinearExpenditureDemandIsAnExplicitAlternative()
  {
    var model = new SmallOpenRegionalTradeModel(
      new SmallOpenRegionalTradeSpecification(
        DemandMechanism: FoodDemandMechanism.LinearExpenditure,
        SubsistenceFoodPerHouseholdPerTick: 0.5m,
        DiscretionaryFoodShare: 0.5m));
    var state = (SmallOpenRegionalTradeState)model.Advance(
      model.CreateState(
        SmallOpenRegionalTradeScenario.Baseline with
        {
          HouseholdOpeningCash = 10m
        }),
      new EconomicTickContext(1, 0, false, [], Seed: 108)).State;

    await Assert.That(state.UnmetFoodDemand).IsLessThan(116m);
    await Assert.That(model.Rules)
      .Contains(rule => rule.Name == "linear-expenditure");
  }

  [Test]
  public async Task CreditMechanismCanBeDisabledWithoutChangingTradeRules()
  {
    var model = new SmallOpenRegionalTradeModel(
      new SmallOpenRegionalTradeSpecification(
        CreditMechanism: CreditMechanism.Disabled));
    var state = (SmallOpenRegionalTradeState)model.Advance(
      model.CreateState(SmallOpenRegionalTradeScenario.Baseline),
      new EconomicTickContext(
        1,
        0,
        false,
        [new DrawWorkingCapitalCommand(100m)],
        Seed: 109)).State;

    await Assert.That(state.CoreState.ClaimState).IsEmpty();
    await Assert.That(model.Rules)
      .Contains(rule => rule.Name == "disabled");
  }
}
