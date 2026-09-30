using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>Opaque state returned by the flagship model.</summary>
public sealed record SmallOpenRegionalTradeState(
    EconomicModelIdentity Model,
    long Tick,
    EconomyState Authority,
    SmallOpenRegionalTradeScenario Scenario,
    decimal ImportedFuel = 0m,
    decimal ExportedFood = 0m,
    decimal ProducedFood = 0m,
    decimal SoldFood = 0m,
    decimal UnmetFoodDemand = 0m) : IEconomicModelState, IEconomicStateValidation
{
    /// <summary>Core authority state for Accounting and diagnostics.</summary>
    public EconomyState CoreState => Authority;

    /// <summary>Stable fingerprint of model state and authoritative positions.</summary>
    public ulong Fingerprint => SmallOpenRegionalTradeModel.Fingerprint(this);

    IReadOnlyList<EconomicValidationSignal>
        IEconomicStateValidation.ValidateState() =>
        InvariantChecker.Check(Authority)
            .Select(violation => new EconomicValidationSignal(
                violation.Code,
                Passed: false,
                violation.Message))
            .ToList();
}
