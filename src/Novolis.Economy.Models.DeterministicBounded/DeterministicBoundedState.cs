using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Models.DeterministicBounded;

/// <summary>Opaque deterministic state.</summary>
public sealed record DeterministicBoundedState(
    EconomicModelIdentity Model,
    long Tick,
    EconomyState Authority,
    DeterministicBoundedScenario Scenario,
    decimal Produced = 0m,
    decimal Sold = 0m,
    decimal UnmetDemand = 0m) : IEconomicModelState, IEconomicStateValidation
{
    public EconomyState CoreState => Authority;

    public ulong Fingerprint => DeterministicBoundedModel.Fingerprint(this);

    IReadOnlyList<EconomicValidationSignal>
        IEconomicStateValidation.ValidateState() =>
        InvariantChecker.Check(Authority)
            .Select(violation => new EconomicValidationSignal(
                violation.Code,
                Passed: false,
                violation.Message))
            .ToList();
}
