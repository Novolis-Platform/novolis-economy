using System.Text.Json;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Core.Invariants;

namespace Novolis.Economy.Models.DeterministicBounded;

/// <summary>Stable JSON codec for the finite deterministic model.</summary>
public sealed class DeterministicBoundedJsonCodec : IEconomicModelCodec
{
    private static readonly JsonSerializerOptions Options =
        EconomicJson.CreateOptions();

    /// <inheritdoc />
    public EconomicModelIdentity Identity { get; } =
        new("DeterministicBounded", "deterministic-bounded-2");

    /// <inheritdoc />
    public EconomicModelSnapshotDocument Capture(
        IEconomicModel model,
        IEconomicScenario scenario,
        IEconomicModelState state,
        EconomicSnapshotCapture capture)
    {
        var typedModel = model as DeterministicBoundedModel
            ?? throw new ArgumentException(
                $"Expected {nameof(DeterministicBoundedModel)}.",
                nameof(model));
        var typedScenario = scenario as DeterministicBoundedScenario
            ?? throw new ArgumentException(
                $"Expected {nameof(DeterministicBoundedScenario)}.",
                nameof(scenario));
        var typedState = state as DeterministicBoundedState
            ?? throw new ArgumentException(
                $"Expected {nameof(DeterministicBoundedState)}.",
                nameof(state));
        EnsureIdentity(typedModel.Identity);
        if (capture.CommandStream.Any(pair => pair.Value.Count > 0))
            throw new InvalidDataException(
                "DeterministicBounded does not accept model commands.");

        var specification = EconomicJson.ToElement(
            typedModel.Specification,
            Options);
        var scenarioPayload = EconomicJson.ToElement(
            typedScenario,
            Options);
        var statePayload = EconomicJson.ToElement(
            typedState,
            Options);
        var commands = Array.Empty<EconomicCommandDocument>();
        var document = new EconomicModelSnapshotDocument(
            EconomicPersistenceSchema.CurrentFormatVersion,
            typedModel.Identity,
            typedScenario.Id,
            typedScenario.Version,
            capture.Seed,
            typedState.Tick,
            capture.Period,
            typedState.Fingerprint,
            specification,
            scenarioPayload,
            statePayload,
            commands,
            capture.Observations,
            capture.Transactions);
        return document with
        {
            SpecificationHash = EconomicJson.HashCanonical(
                typedModel.Specification,
                Options),
            CommandHash = EconomicJson.HashCanonical(commands, Options)
        };
    }

    /// <inheritdoc />
    public EconomicModelRestore Restore(
        EconomicModelSnapshotDocument document)
    {
        EconomicJson.RequireCurrentSchema(document);
        EnsureIdentity(document.Model);
        if (document.Commands.Count > 0)
            throw new InvalidDataException(
                "DeterministicBounded snapshots cannot contain commands.");

        var specification = document.Specification.Deserialize<
            DeterministicBoundedSpecification>(Options)
            ?? throw new InvalidDataException("Missing bounded specification.");
        var scenario = document.Scenario.Deserialize<
            DeterministicBoundedScenario>(Options)
            ?? throw new InvalidDataException("Missing bounded scenario.");
        var state = document.State.Deserialize<DeterministicBoundedState>(Options)
            ?? throw new InvalidDataException("Missing bounded state.");
        var model = new DeterministicBoundedModel(specification);

        if (state.Model != document.Model ||
            state.Tick != document.Tick ||
            state.Scenario != scenario ||
            document.ScenarioId != scenario.Id ||
            document.ScenarioVersion != scenario.Version ||
            (state.Tick > 0 &&
             document.Seed != state.Authority.SimulationSeed) ||
            document.Period != state.Authority.Period ||
            !string.Equals(
                document.SpecificationHash,
                EconomicJson.HashCanonical(specification, Options),
                StringComparison.Ordinal) ||
            !string.Equals(
                document.CommandHash,
                EconomicJson.HashCanonical(document.Commands, Options),
                StringComparison.Ordinal) ||
            state.Fingerprint != document.StateFingerprint)
        {
            throw new InvalidDataException(
                "Bounded snapshot metadata does not match its state payload.");
        }

        PersistenceInvariantChecker.AssertRestorable(state.CoreState);
        return new EconomicModelRestore(
            model,
            scenario,
            state,
            new SortedDictionary<
                long,
                IReadOnlyList<IEconomicModelCommand>>(),
            document.Observations,
            document.Transactions);
    }

    private void EnsureIdentity(EconomicModelIdentity identity)
    {
        if (identity != Identity)
        {
            throw new InvalidDataException(
                $"Codec '{Identity}' cannot handle model '{identity}'.");
        }
    }
}
