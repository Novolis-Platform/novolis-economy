using Novolis.Economy.Accounting;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Models.SmallOpenRegionalTrade;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Integration.Research;

public sealed class ResearchModelIntegrationTests
{
    [Test]
    public async Task ResearchRunCanInspectFinancialsAndTransactionHistory()
    {
        var model = new SmallOpenRegionalTradeModel();
        var state = model.CreateState(SmallOpenRegionalTradeScenario.Baseline);
        var result = model.Advance(
            state,
            new EconomicTickContext(1, 0, false, [], Seed: 7));
        var next = (SmallOpenRegionalTradeState)result.State;

        var projection = AccountingQuery.Project(
            next.CoreState,
            new FinancialScope.EntityKind(
                Novolis.Economy.Core.LegalEntityKind.Firm));

        await Assert.That(projection.EntityBooks).IsNotEmpty();
        await Assert.That(projection.Transactions).IsNotEmpty();
        await Assert.That(projection.IsBalanced).IsTrue();
        await Assert.That(next.CoreState.Journal.Count)
            .IsEqualTo(projection.Transactions.Count);
    }

    [Test]
    public async Task ResearchParameterChangeProducesAnExplainableDifference()
    {
        var open = new SmallOpenRegionalTradeModel(
            new SmallOpenRegionalTradeSpecification(
                ImportCapacityPerTick: 12m));
        var restricted = new SmallOpenRegionalTradeModel(
            new SmallOpenRegionalTradeSpecification(
                ImportCapacityPerTick: 0m));

        var openState = open.CreateState(SmallOpenRegionalTradeScenario.Baseline);
        var restrictedState = restricted.CreateState(
            SmallOpenRegionalTradeScenario.Baseline);
        var openResult = (SmallOpenRegionalTradeState)open.Advance(
            openState,
            new EconomicTickContext(1, 0, false, [], Seed: 11)).State;
        var restrictedResult = (SmallOpenRegionalTradeState)restricted.Advance(
            restrictedState,
            new EconomicTickContext(1, 0, false, [], Seed: 11)).State;

        await Assert.That(openResult.ImportedFuel)
            .IsGreaterThan(restrictedResult.ImportedFuel);
        await Assert.That(openResult.Model.Version)
            .IsEqualTo(restrictedResult.Model.Version);
        await Assert.That(openResult.Fingerprint)
            .IsNotEqualTo(restrictedResult.Fingerprint);
    }

    [Test]
    public async Task ResearchConsumerCanRunValidationAndExportTheReport()
    {
        var report = EconomicValidationRunner.Validate(
            new EconomicValidationRequest(
                new SmallOpenRegionalTradeModel(),
                SmallOpenRegionalTradeScenario.Baseline,
                Seed: 19,
                Ticks: 3,
                Plan: new CalibrationPlan(
                    "research-baseline",
                    "1",
                    [
                        new CalibrationTarget(
                            "invariants",
                            "core-invariants",
                            "boolean",
                            CalibrationTargetKind.InternalInvariant,
                            "small-open-regional-trade-3",
                            "implementation",
                            TargetValue: 1m),
                        new CalibrationTarget(
                            "clearing",
                            "food-market-clearing-rate",
                            "ratio",
                            CalibrationTargetKind.Empirical,
                            "small-open-regional-trade-3",
                            "illustrative",
                            Minimum: 0m,
                            Maximum: 1m)])));

        var restored = EconomicRunStore.DeserializeValidationReport(
            EconomicRunStore.Serialize(report));

        await Assert.That(restored.Passed).IsTrue();
        await Assert.That(restored.Measurements).Count().IsEqualTo(2);
        await Assert.That(restored.SpecificationHash).IsNotEmpty();
    }
}
