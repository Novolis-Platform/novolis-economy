using Novolis.Economy.Abstractions;
using Novolis.Economy.Models.SmallOpenRegionalTrade;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Integration.Game;

public sealed class GameModelIntegrationTests
{
    [Test]
    public async Task GameClockAdvancesModelWithoutSimulationAssembly()
    {
        var model = new SmallOpenRegionalTradeModel();
        var state = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);

        var result = model.Advance(
            state,
            new EconomicTickContext(
                Tick: 1,
                Period: 0,
                IsPeriodBoundary: false,
                Commands: [new PurchaseFoodCommand(1m)],
                Seed: 42));

        var next = (SmallOpenRegionalTradeState)result.State;
        await Assert.That(next.Tick).IsEqualTo(1);
        await Assert.That(next.ProducedFood).IsGreaterThan(0m);
        await Assert.That(next.ImportedFuel).IsGreaterThan(0m);
        await Assert.That(next.SoldFood).IsGreaterThan(0m);
        await Assert.That(next.UnmetFoodDemand).IsGreaterThan(0m);
        await Assert.That(result.Transactions).IsNotEmpty();
        await Assert.That(result.Observations)
            .Contains(observation => observation.Name == "food-market-clearing-rate");
    }

    [Test]
    public async Task GameActionIsSettledThroughCorePositions()
    {
        var model = new SmallOpenRegionalTradeModel();
        var before = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);

        var result = model.Advance(
            before,
            new EconomicTickContext(
                1,
                0,
                false,
                [new PurchaseFoodCommand(1m)],
                Seed: 42));

        var after = (SmallOpenRegionalTradeState)result.State;
        var monetaryAsset = after.CoreState.MonetaryAssetId;
        var household = EconomicEntityId.From(
            Guid.Parse("3d3e3f40-0000-4000-8000-000000000001"));
        var beforeCash = before.CoreState.PositionState.Values
            .Where(position => position.Owner == household)
            .Where(position => position.Asset == monetaryAsset)
            .Sum(position => position.Quantity);
        var afterCash = after.CoreState.PositionState.Values
            .Where(position => position.Owner == household)
            .Where(position => position.Asset == monetaryAsset)
            .Sum(position => position.Quantity);

        await Assert.That(afterCash).IsLessThan(beforeCash);
        await Assert.That(after.CoreState.Journal).IsNotEmpty();
        await Assert.That(after.Fingerprint).IsNotEqualTo(before.Fingerprint);
    }
}
