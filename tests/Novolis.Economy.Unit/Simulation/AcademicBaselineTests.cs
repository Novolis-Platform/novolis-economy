using Novolis.Economy.Abstractions;
using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Models.SmallOpenRegionalTrade;

namespace Novolis.Economy.Unit.Simulation;

public sealed class AcademicBaselineTests
{
  [Test]
  public async Task DefaultUnitOfAccountIsStableAndNotTheZeroAsset()
  {
    await Assert.That(EconomyState.DefaultUnitOfAccountAssetId.Value)
      .IsNotEqualTo(Guid.Empty);
    await Assert.That(EconomyState.Empty.MonetaryAssetId)
      .IsEqualTo(EconomyState.DefaultUnitOfAccountAssetId);
  }

  [Test]
  public async Task WorkingCapitalCreatesAClaimAndConservesCash()
  {
    var model = new SmallOpenRegionalTradeModel();
    var before = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);
    var beforeCash = CashTotal(before.CoreState);

    var result = (SmallOpenRegionalTradeState)model.Advance(
      before,
      new EconomicTickContext(
        1,
        0,
        false,
        [new DrawWorkingCapitalCommand(500m)],
        Seed: 77)).State;

    await Assert.That(result.CoreState.ClaimState.Values)
      .Contains(claim => claim.Principal.Quantity == 500m);
    await Assert.That(CashTotal(result.CoreState))
      .IsEqualTo(beforeCash);
    await Assert.That(result.CoreState.Journal)
      .Contains(transaction => transaction.Reason == "working-capital-origination");
  }

  [Test]
  public async Task PeriodBoundaryAccruesDeclaredInterest()
  {
    var model = new SmallOpenRegionalTradeModel();
    var state = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);
    state = (SmallOpenRegionalTradeState)model.Advance(
      state,
      new EconomicTickContext(
        1,
        0,
        false,
        [new DrawWorkingCapitalCommand(500m)],
        Seed: 78)).State;
    var beforeBoundary = state.CoreState.ClaimState.Values
      .Single(claim => claim.Debtor.Value ==
                       Guid.Parse("3d3e3f40-0000-4000-8000-000000000002"))
      .Principal.Quantity;

    for (var tick = 2L; tick <= 24; tick++)
    {
      state = (SmallOpenRegionalTradeState)model.Advance(
        state,
        new EconomicTickContext(
          tick,
          (int)((tick - 1) / 24),
          tick % 24 == 0,
          [],
          Seed: 78)).State;
    }

    var afterBoundary = state.CoreState.ClaimState.Values
      .Single(claim => claim.Debtor.Value ==
                       Guid.Parse("3d3e3f40-0000-4000-8000-000000000002"))
      .Principal.Quantity;
    await Assert.That(afterBoundary).IsGreaterThan(beforeBoundary);
  }

  [Test]
  public async Task AccountingExternalSectorScopeIsReadOnlyAndExplainable()
  {
    var model = new SmallOpenRegionalTradeModel();
    var state = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);
    var beforeFingerprint = state.Fingerprint;
    state = (SmallOpenRegionalTradeState)model.Advance(
      state,
      new EconomicTickContext(1, 0, false, [], Seed: 79)).State;

    var projection = AccountingQuery.Project(
      state.CoreState,
      new FinancialScope.ExternalSector());

    await Assert.That(projection.EntityBooks).IsNotEmpty();
    await Assert.That(projection.Transactions).IsNotEmpty();
    await Assert.That(state.Fingerprint).IsNotEqualTo(beforeFingerprint);
    await Assert.That(projection.IsBalanced).IsTrue();
  }

  private static decimal CashTotal(EconomyState state) =>
    state.PositionState.Values
      .Where(position => position.Asset == state.MonetaryAssetId)
      .Sum(position => position.Quantity);
}
