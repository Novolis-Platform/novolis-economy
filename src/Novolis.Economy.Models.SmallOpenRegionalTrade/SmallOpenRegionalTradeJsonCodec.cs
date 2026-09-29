using System.Text.Json;
using Novolis.Economy.Abstractions;
using Novolis.Economy.Core.Invariants;

namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>
/// Stable JSON codec for the flagship model and its typed command stream.
/// </summary>
public sealed class SmallOpenRegionalTradeJsonCodec : IEconomicModelCodec
{
    private static readonly JsonSerializerOptions Options =
        EconomicJson.CreateOptions();

    /// <inheritdoc />
    public EconomicModelIdentity Identity { get; } =
        new("SmallOpenRegionalTrade", "small-open-regional-trade-3");

    /// <inheritdoc />
    public EconomicModelSnapshotDocument Capture(
        IEconomicModel model,
        IEconomicScenario scenario,
        IEconomicModelState state,
        EconomicSnapshotCapture capture)
    {
        var typedModel = model as SmallOpenRegionalTradeModel
            ?? throw new ArgumentException(
                $"Expected {nameof(SmallOpenRegionalTradeModel)}.",
                nameof(model));
        var typedScenario = scenario as SmallOpenRegionalTradeScenario
            ?? throw new ArgumentException(
                $"Expected {nameof(SmallOpenRegionalTradeScenario)}.",
                nameof(scenario));
        var typedState = state as SmallOpenRegionalTradeState
            ?? throw new ArgumentException(
                $"Expected {nameof(SmallOpenRegionalTradeState)}.",
                nameof(state));
        EnsureIdentity(typedModel.Identity);

        var specification = EconomicJson.ToElement(
            typedModel.Specification,
            Options);
        var scenarioPayload = EconomicJson.ToElement(
            typedScenario,
            Options);
        var statePayload = EconomicJson.ToElement(
            typedState,
            Options);
        var commands = SerializeCommands(capture.CommandStream);
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

        var specification = document.Specification.Deserialize<
            SmallOpenRegionalTradeSpecification>(Options)
            ?? throw new InvalidDataException("Missing flagship specification.");
        var scenario = document.Scenario.Deserialize<
            SmallOpenRegionalTradeScenario>(Options)
            ?? throw new InvalidDataException("Missing flagship scenario.");
        var state = document.State.Deserialize<SmallOpenRegionalTradeState>(Options)
            ?? throw new InvalidDataException("Missing flagship state.");
        var model = new SmallOpenRegionalTradeModel(specification);
        var commands = DeserializeCommands(document.Commands);

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
                "Flagship snapshot metadata does not match its state payload.");
        }

        PersistenceInvariantChecker.AssertRestorable(state.CoreState);
        return new EconomicModelRestore(
            model,
            scenario,
            state,
            commands,
            document.Observations,
            document.Transactions);
    }

    private static IReadOnlyList<EconomicCommandDocument> SerializeCommands(
        IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>> commandStream)
    {
        var documents = new List<EconomicCommandDocument>();
        foreach (var (tick, commands) in commandStream.OrderBy(item => item.Key))
        {
            foreach (var command in commands)
            {
                if (command is not (PurchaseFoodCommand or DrawWorkingCapitalCommand))
                    throw new InvalidDataException(
                        $"Unsupported flagship command '{command.Kind}'.");

                documents.Add(new EconomicCommandDocument(
                    tick,
                    command.Kind,
                    EconomicJson.ToElement(command, Options)));
            }
        }

        return documents;
    }

    private static IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>>
        DeserializeCommands(
            IReadOnlyList<EconomicCommandDocument> documents)
    {
        var commands = new SortedDictionary<
            long,
            IReadOnlyList<IEconomicModelCommand>>();
        foreach (var document in documents.OrderBy(item => item.Tick))
        {
            IEconomicModelCommand command = document.Kind switch
            {
                "purchase-food" =>
                    document.Payload.Deserialize<PurchaseFoodCommand>(Options)
                    ?? throw new InvalidDataException("Invalid food command."),
                "draw-working-capital" =>
                    document.Payload.Deserialize<DrawWorkingCapitalCommand>(Options)
                    ?? throw new InvalidDataException("Invalid credit command."),
                _ => throw new InvalidDataException(
                    $"Unsupported flagship command '{document.Kind}'.")
            };

            var existing = commands.GetValueOrDefault(document.Tick)
                ?? Array.Empty<IEconomicModelCommand>();
            commands[document.Tick] = existing
                .Append(command)
                .ToArray();
        }

        return commands;
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
