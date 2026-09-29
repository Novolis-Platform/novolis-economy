using System.Text.Json;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Models.SmallOpenRegionalTrade;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Unit.Simulation;

public sealed class CalibrationValidationTests
{
    [Test]
    public async Task CalibrationPlanIsSerializableWithItsEvidenceMetadata()
    {
        var plan = new CalibrationPlan(
            "small-open-baseline",
            "1",
            [
                new CalibrationTarget(
                    "invariants",
                    "core-invariants",
                    "boolean",
                    CalibrationTargetKind.InternalInvariant,
                    "small-open-regional-trade-3",
                    "implementation",
                    TargetValue: 1m,
                    Description: "Core authority must remain internally valid."),
                new CalibrationTarget(
                    "clearing-rate",
                    "food-market-clearing-rate",
                    "ratio",
                    CalibrationTargetKind.Empirical,
                    "small-open-regional-trade-3",
                    "teaching-target",
                    Minimum: 0m,
                    Maximum: 1m,
                    Tolerance: 0.001m)
            ]);

        var json = EconomicJson.SerializeCanonical(
            plan,
            EconomicJson.CreateOptions());
        var restored = JsonSerializer.Deserialize<CalibrationPlan>(
            json,
            EconomicJson.CreateOptions());

        await Assert.That(restored!.Id).IsEqualTo(plan.Id);
        await Assert.That(restored.Version).IsEqualTo(plan.Version);
        await Assert.That(restored.Targets.Count).IsEqualTo(plan.Targets.Count);
        for (var index = 0; index < plan.Targets.Count; index++)
        {
            await Assert.That(restored.Targets[index])
                .IsEqualTo(plan.Targets[index]);
        }
        await Assert.That(json).Contains("\"source\":\"implementation\"");
        await Assert.That(json).Contains("\"kind\":\"Empirical\"");
    }

    [Test]
    public async Task ValidationRunnerReportsInternalAndEmpiricalTargetsSeparately()
    {
        var report = EconomicValidationRunner.Validate(
            new EconomicValidationRequest(
                new SmallOpenRegionalTradeModel(),
                SmallOpenRegionalTradeScenario.Baseline,
                Seed: 2026,
                Ticks: 1,
                Plan: new CalibrationPlan(
                    "baseline-validation",
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
                            "illustrative-range",
                            Minimum: 0m,
                            Maximum: 1m)
                    ])));

        await Assert.That(report.Passed).IsTrue();
        await Assert.That(report.Invariants).IsEmpty();
        await Assert.That(report.Measurements).Count().IsEqualTo(2);
        await Assert.That(report.Measurements
                .Single(item => item.Target.Kind == CalibrationTargetKind.InternalInvariant)
                .Passed)
            .IsTrue();
        await Assert.That(report.Measurements
                .Single(item => item.Target.Kind == CalibrationTargetKind.Empirical)
                .ActualValue)
            .IsNotNull();
    }

    [Test]
    public async Task ValidationRunnerDoesNotPretendAnUnmetEmpiricalTargetIsAnInvariant()
    {
        var report = EconomicValidationRunner.Validate(
            new EconomicValidationRequest(
                new SmallOpenRegionalTradeModel(),
                SmallOpenRegionalTradeScenario.Baseline,
                Seed: 2027,
                Ticks: 1,
                Plan: new CalibrationPlan(
                    "deliberately-unmet",
                    "1",
                    [
                        new CalibrationTarget(
                            "price",
                            "food-price",
                            "unit-of-account/unit",
                            CalibrationTargetKind.Empirical,
                            "small-open-regional-trade-3",
                            "test",
                            TargetValue: 999m)
                    ])));

        await Assert.That(report.Passed).IsFalse();
        await Assert.That(report.Invariants).IsEmpty();
        await Assert.That(report.Measurements.Single().Target.Kind)
            .IsEqualTo(CalibrationTargetKind.Empirical);
        await Assert.That(report.Measurements.Single().Passed).IsFalse();
    }
}
