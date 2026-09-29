using Novolis.Economy.Abstractions;
using Novolis.Economy.Models.SmallOpenRegionalTrade;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit.Simulation;

public sealed class ResearchRunWorkflowTests
{
    [Test]
    public async Task RunRecordsRoundTripWithoutPersistingOpaqueClrState()
    {
        var run = EconomicModelRunner.Run(
            new EconomicModelRunRequest(
                new SmallOpenRegionalTradeModel(),
                SmallOpenRegionalTradeScenario.Baseline,
                Seed: 11,
                Ticks: 2));

        var json = EconomicRunStore.Serialize(run);
        var restored = EconomicRunStore.Deserialize(json);

        await Assert.That(json).Contains("\"economy.run.v1\"");
        await Assert.That(json).DoesNotContain("SmallOpenRegionalTradeState,");
        await Assert.That(restored.Manifest.Model).IsEqualTo(run.Manifest.Model);
        await Assert.That(restored.Manifest.FinalStateFingerprint)
            .IsEqualTo(run.Manifest.FinalStateFingerprint);
        await Assert.That(restored.Observations.Count)
            .IsEqualTo(run.Observations.Count);
        await Assert.That(restored.Transactions.Count)
            .IsEqualTo(run.Transactions.Count);
    }

    [Test]
    public async Task MultiSeedAndComparisonRetainDeclaredSeedIdentity()
    {
        var runs = EconomicModelRunner.RunSeeds(
            seed => new EconomicModelRunRequest(
                new SmallOpenRegionalTradeModel(),
                SmallOpenRegionalTradeScenario.Baseline,
                seed,
                Ticks: 1),
            [1UL, 2UL, 3UL]);

        var comparison = EconomicModelRunner.Compare(
            runs.Select((run, index) => ($"seed-{index + 1}", run)),
            "food-sold");

        await Assert.That(runs.Select(run => run.Manifest.Seed).ToArray())
            .IsEquivalentTo([1UL, 2UL, 3UL]);
        await Assert.That(comparison.Values.Count).IsEqualTo(3);
        await Assert.That(comparison.Minimum).IsLessThanOrEqualTo(comparison.Maximum);
        await Assert.That(comparison.Spread).IsEqualTo(0m);
    }

    [Test]
    public async Task NamedVariantsProduceAResearchSensitivitySet()
    {
        var variants = EconomicModelRunner.RunVariants(
            [
                (
                    "posted",
                    new EconomicModelRunRequest(
                        new SmallOpenRegionalTradeModel(),
                        SmallOpenRegionalTradeScenario.Baseline,
                        Seed: 0,
                        Ticks: 1)),
                (
                    "discovery",
                    new EconomicModelRunRequest(
                        new SmallOpenRegionalTradeModel(
                            new SmallOpenRegionalTradeSpecification(
                                MarketMechanism:
                                FoodMarketMechanism.SupplyDemandPriceDiscovery)),
                        SmallOpenRegionalTradeScenario.Baseline,
                        Seed: 0,
                        Ticks: 1))
            ],
            [9UL, 10UL]);

        await Assert.That(variants.Select(variant => variant.VariantId).ToArray())
            .IsEquivalentTo(["posted", "discovery"]);
        await Assert.That(variants.SelectMany(variant => variant.Runs).Count())
            .IsEqualTo(4);
    }

    [Test]
    public async Task ContinuedRunStartsAtRestoredTickAndMatchesFreshSuffix()
    {
        var model = new SmallOpenRegionalTradeModel();
        var scenario = SmallOpenRegionalTradeScenario.Baseline;
        var first = EconomicModelRunner.Run(
            new EconomicModelRunRequest(model, scenario, 12, Ticks: 3));

        var continued = EconomicModelRunner.Continue(
            new EconomicModelRunRequest(
                model,
                scenario,
                Seed: 12,
                Ticks: 2),
            first.State);
        var fresh = EconomicModelRunner.Run(
            new EconomicModelRunRequest(model, scenario, 12, Ticks: 5));

        await Assert.That(continued.Manifest.InitialTick).IsEqualTo(3);
        await Assert.That(continued.State.Fingerprint)
            .IsEqualTo(fresh.State.Fingerprint);
        await Assert.That(continued.Manifest.FinalStateFingerprint)
            .IsEqualTo(fresh.State.Fingerprint);
    }

    [Test]
    public async Task ValidationReportsHaveAJsonRoundTrip()
    {
        var report = EconomicValidationRunner.Validate(
            new EconomicValidationRequest(
                new SmallOpenRegionalTradeModel(),
                SmallOpenRegionalTradeScenario.Baseline,
                Seed: 13,
                Ticks: 1,
                Plan: new CalibrationPlan(
                    "run-report",
                    "1",
                    [
                        new CalibrationTarget(
                            "invariants",
                            "core-invariants",
                            "boolean",
                            CalibrationTargetKind.InternalInvariant,
                            "small-open-regional-trade-3",
                            "implementation",
                            TargetValue: 1m)
                    ])));

        var restored = EconomicRunStore.DeserializeValidationReport(
            EconomicRunStore.Serialize(report));

        await Assert.That(restored.Model).IsEqualTo(report.Model);
        await Assert.That(restored.Passed).IsEqualTo(report.Passed);
        await Assert.That(restored.Measurements.Count)
            .IsEqualTo(report.Measurements.Count);
    }
}
