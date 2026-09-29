using Novolis.Economy.Accounting;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Models.DeterministicBounded;
using Novolis.Economy.Models.SmallOpenRegionalTrade;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Integration.Simulation;

public sealed class SimulationModelIntegrationTests
{
    [Test]
    public async Task GenericRunnerReplaysTheSameModelDeterministically()
    {
        var left = EconomicModelRunner.Run(
            new EconomicModelRunRequest(
                new SmallOpenRegionalTradeModel(),
                SmallOpenRegionalTradeScenario.Baseline,
                Seed: 314159,
                Ticks: 24));
        var right = EconomicModelRunner.Run(
            new EconomicModelRunRequest(
                new SmallOpenRegionalTradeModel(),
                SmallOpenRegionalTradeScenario.Baseline,
                Seed: 314159,
                Ticks: 24));

        await Assert.That(left.State.Fingerprint)
            .IsEqualTo(right.State.Fingerprint);
        await Assert.That(left.Manifest.SpecificationHash)
            .IsEqualTo(right.Manifest.SpecificationHash);
        await Assert.That(left.Transactions.Count)
            .IsEqualTo(right.Transactions.Count);
    }

    [Test]
    public async Task GenericRunnerDoesNotNeedConcreteModelKnowledge()
    {
        var result = EconomicModelRunner.Run(
            new EconomicModelRunRequest(
                new DeterministicBoundedModel(),
                DeterministicBoundedScenario.Baseline,
                Seed: 9,
                Ticks: 24));

        await Assert.That(result.Manifest.Model.Id)
            .IsEqualTo("DeterministicBounded");
        await Assert.That(result.State.Fingerprint)
            .IsNotEqualTo(result.Manifest.InitialStateFingerprint);
        await Assert.That(result.Observations)
            .Contains(observation => observation.Name == "food-market-clearing-rate");
    }

    [Test]
    public async Task GameSimulationAndResearchConsumersProduceTheSameModelState()
    {
        const ulong seed = 271828;
        const long ticks = 24;
        var commands = new Dictionary<long, IReadOnlyList<IEconomicModelCommand>>
        {
            [1] = [new PurchaseFoodCommand(1m)]
        };

        var gameModel = new SmallOpenRegionalTradeModel();
        var gameState = gameModel.CreateState(SmallOpenRegionalTradeScenario.Baseline);
        for (var tick = 1L; tick <= ticks; tick++)
        {
            gameState = (SmallOpenRegionalTradeState)gameModel.Advance(
                gameState,
                CreateContext(tick, seed, commands)).State;
        }

        var simulationRun = EconomicModelRunner.Run(
            new EconomicModelRunRequest(
                new SmallOpenRegionalTradeModel(),
                SmallOpenRegionalTradeScenario.Baseline,
                seed,
                ticks,
                Commands: commands));

        var researchModel = new SmallOpenRegionalTradeModel();
        var researchState = researchModel.CreateState(
            SmallOpenRegionalTradeScenario.Baseline);
        for (var tick = 1L; tick <= ticks; tick++)
        {
            researchState = (SmallOpenRegionalTradeState)researchModel.Advance(
                researchState,
                CreateContext(tick, seed, commands)).State;
        }

        var financials = AccountingQuery.Project(
            researchState.CoreState,
            new FinancialScope.Group(
                researchState.CoreState.Entities.Keys.ToHashSet()));

        await Assert.That(gameState.Fingerprint)
            .IsEqualTo(simulationRun.State.Fingerprint);
        await Assert.That(researchState.Fingerprint)
            .IsEqualTo(simulationRun.State.Fingerprint);
        await Assert.That(financials.IsBalanced).IsTrue();
        await Assert.That(simulationRun.Transactions).IsNotEmpty();
    }

    [Test]
    public async Task SimulationConsumerCanPersistRunEvidenceAndRestoreSnapshot()
    {
        var model = new SmallOpenRegionalTradeModel();
        var scenario = SmallOpenRegionalTradeScenario.Baseline;
        var run = EconomicModelRunner.Run(
            new EconomicModelRunRequest(model, scenario, Seed: 42, Ticks: 2));

        var runRecord = EconomicRunStore.Deserialize(
            EconomicRunStore.Serialize(run));
        var registry = new EconomicModelCodecRegistry();
        registry.Register(new SmallOpenRegionalTradeJsonCodec());
        var store = new EconomicSnapshotStore(registry);
        var snapshot = store.Capture(
            new SmallOpenRegionalTradeJsonCodec(),
            model,
            scenario,
            run.State,
            new EconomicSnapshotCapture(
                42,
                run.State.Tick,
                ((SmallOpenRegionalTradeState)run.State).CoreState.Period,
                new Dictionary<long, IReadOnlyList<IEconomicModelCommand>>(),
                run.Observations,
                run.Transactions));
        var restored = store.Restore(store.Serialize(snapshot));
        var restoredState = (SmallOpenRegionalTradeState)restored.State;
        var restoredFinancials = AccountingQuery.Project(
            restoredState.CoreState,
            new FinancialScope.Group(
                restoredState.CoreState.Entities.Keys.ToHashSet()));

        await Assert.That(runRecord.Manifest.FinalStateFingerprint)
            .IsEqualTo(run.State.Fingerprint);
        await Assert.That(restored.State.Fingerprint)
            .IsEqualTo(run.State.Fingerprint);
        await Assert.That(restoredFinancials.IsBalanced).IsTrue();
    }

    private static EconomicTickContext CreateContext(
        long tick,
        ulong seed,
        IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>> commands) =>
        new(
            tick,
            checked((int)((tick - 1) / 24)),
            tick % 24 == 0,
            commands.GetValueOrDefault(tick) ?? [],
            Seed: seed);
}
