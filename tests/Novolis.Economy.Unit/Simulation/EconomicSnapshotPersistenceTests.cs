using Novolis.Economy.Abstractions;
using Novolis.Economy.Models.DeterministicBounded;
using Novolis.Economy.Models.SmallOpenRegionalTrade;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit.Simulation;

public sealed class EconomicSnapshotPersistenceTests
{
    [Test]
    public async Task SmallOpenRegionalTradeSnapshot_RoundTripsWithStableFingerprint()
    {
        var model = new SmallOpenRegionalTradeModel();
        var scenario = SmallOpenRegionalTradeScenario.Baseline;
        var state = model.CreateState(scenario);
        var commands = new Dictionary<long, IReadOnlyList<IEconomicModelCommand>>
        {
            [1] =
            [
                new PurchaseFoodCommand(1m),
                new DrawWorkingCapitalCommand(100m)
            ]
        };
        var transactions = new List<EconomicTransitionReceipt>();

        for (var tick = 1L; tick <= 3; tick++)
        {
            var result = model.Advance(
                state,
                new EconomicTickContext(
                    tick,
                    0,
                    false,
                    commands.GetValueOrDefault(tick) ?? [],
                    Seed: 42));
            state = (SmallOpenRegionalTradeState)result.State;
            transactions.AddRange(result.Transactions);
        }

        var registry = new EconomicModelCodecRegistry();
        registry.Register(new SmallOpenRegionalTradeJsonCodec());
        var store = new EconomicSnapshotStore(registry);
        var document = store.Capture(
            new SmallOpenRegionalTradeJsonCodec(),
            model,
            scenario,
            state,
            new EconomicSnapshotCapture(
                42,
                state.Tick,
                state.Authority.Period,
                commands,
                [],
                transactions));

        var json = store.Serialize(document);
        var restored = store.Restore(json);
        var restoredState = (SmallOpenRegionalTradeState)restored.State;

        await Assert.That(json).Contains("\"formatVersion\"");
        await Assert.That(json).DoesNotContain("SmallOpenRegionalTradeState,");
        await Assert.That(restoredState.Fingerprint)
            .IsEqualTo(state.Fingerprint);
        await Assert.That(restoredState.CoreState.PositionState.Keys)
            .IsEquivalentTo(state.CoreState.PositionState.Keys);
        foreach (var key in state.CoreState.PositionState.Keys)
        {
            await Assert.That(restoredState.CoreState.PositionState[key])
                .IsEqualTo(state.CoreState.PositionState[key]);
        }
        await Assert.That(restoredState.CoreState.ClaimState.Keys)
            .IsEquivalentTo(state.CoreState.ClaimState.Keys);
        foreach (var key in state.CoreState.ClaimState.Keys)
        {
            await Assert.That(restoredState.CoreState.ClaimState[key])
                .IsEqualTo(state.CoreState.ClaimState[key]);
        }
        await Assert.That(restoredState.CoreState.Journal.Count)
            .IsEqualTo(state.CoreState.Journal.Count);
        var originalJournalIds = state.CoreState.Journal.Select(item => item.Id).ToArray();
        var restoredJournalIds = restoredState.CoreState.Journal.Select(item => item.Id).ToArray();
        for (var index = 0; index < originalJournalIds.Length; index++)
        {
            await Assert.That(restoredJournalIds[index])
                .IsEqualTo(originalJournalIds[index]);
        }
        await Assert.That(restored.CommandStream[1].Count()).IsEqualTo(2);

        var originalSuffix = (SmallOpenRegionalTradeState)model.Advance(
            state,
            new EconomicTickContext(4, 0, false, [], Seed: 42)).State;
        var restoredSuffix = (SmallOpenRegionalTradeState)restored.Model.Advance(
            restoredState,
            new EconomicTickContext(4, 0, false, [], Seed: 42)).State;

        await Assert.That(restoredSuffix.Fingerprint)
            .IsEqualTo(originalSuffix.Fingerprint);
        var originalSuffixJournalIds = originalSuffix.CoreState.Journal
            .Select(item => item.Id)
            .ToArray();
        var restoredSuffixJournalIds = restoredSuffix.CoreState.Journal
            .Select(item => item.Id)
            .ToArray();
        for (var index = 0; index < originalSuffixJournalIds.Length; index++)
        {
            await Assert.That(restoredSuffixJournalIds[index])
                .IsEqualTo(originalSuffixJournalIds[index]);
        }
    }

    [Test]
    public async Task DeterministicBoundedSnapshot_RoundTripsWithoutCommands()
    {
        var model = new DeterministicBoundedModel();
        var scenario = DeterministicBoundedScenario.Baseline;
        var state = model.CreateState(scenario);
        state = (DeterministicBoundedState)model.Advance(
            state,
            new EconomicTickContext(1, 0, false, [], Seed: 9)).State;

        var registry = new EconomicModelCodecRegistry();
        registry.Register(new DeterministicBoundedJsonCodec());
        var store = new EconomicSnapshotStore(registry);
        var document = store.Capture(
            new DeterministicBoundedJsonCodec(),
            model,
            scenario,
            state,
            new EconomicSnapshotCapture(
                9,
                state.Tick,
                state.Authority.Period,
                new Dictionary<long, IReadOnlyList<IEconomicModelCommand>>(),
                [],
                []));

        var restored = store.Restore(store.Serialize(document));
        var restoredState = (DeterministicBoundedState)restored.State;

        await Assert.That(restoredState.Fingerprint)
            .IsEqualTo(state.Fingerprint);
        await Assert.That(restoredState.CoreState.PositionState.Keys)
            .IsEquivalentTo(state.CoreState.PositionState.Keys);
        foreach (var key in state.CoreState.PositionState.Keys)
        {
            await Assert.That(restoredState.CoreState.PositionState[key])
                .IsEqualTo(state.CoreState.PositionState[key]);
        }
        var boundedJournalIds = state.CoreState.Journal.Select(item => item.Id).ToArray();
        var restoredBoundedJournalIds = restoredState.CoreState.Journal
            .Select(item => item.Id)
            .ToArray();
        for (var index = 0; index < boundedJournalIds.Length; index++)
        {
            await Assert.That(restoredBoundedJournalIds[index])
                .IsEqualTo(boundedJournalIds[index]);
        }
        await Assert.That(restored.CommandStream).IsEmpty();
    }

    [Test]
    public async Task SnapshotStoreRejectsUnknownModelVersion()
    {
        var registry = new EconomicModelCodecRegistry();
        registry.Register(new SmallOpenRegionalTradeJsonCodec());
        var store = new EconomicSnapshotStore(registry);
        var model = new SmallOpenRegionalTradeModel();
        var scenario = SmallOpenRegionalTradeScenario.Baseline;
        var state = model.CreateState(scenario);
        var document = store.Capture(
            new SmallOpenRegionalTradeJsonCodec(),
            model,
            scenario,
            state,
            new EconomicSnapshotCapture(1, 0, 0, new Dictionary<
                long,
                IReadOnlyList<IEconomicModelCommand>>(), [], []));
        var invalid = document with
        {
            Model = document.Model with { Version = "future-model" }
        };

        var act = () => store.Restore(store.Serialize(invalid));

        await Assert.That(act).Throws<InvalidDataException>();
    }
}
