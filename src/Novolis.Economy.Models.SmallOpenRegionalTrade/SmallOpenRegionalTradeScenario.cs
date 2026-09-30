using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Models.SmallOpenRegionalTrade;

/// <summary>Named initial conditions for the flagship model.</summary>
public sealed record SmallOpenRegionalTradeScenario(
    string Id = "three-region-baseline",
    string Version = "small-open-regional-trade-scenario-1",
    int RegionCount = 3,
    int HouseholdCount = 120,
    decimal HouseholdOpeningCash = 100m,
    decimal ProducerOpeningCash = 1_000m,
    decimal ExternalOpeningCash = 100_000m,
    decimal ExternalFuelStock = 10_000m,
    decimal BankOpeningCash = 50_000m) : IEconomicScenario
{
    /// <summary>Canonical scenario used by examples and integration tests.</summary>
    public static SmallOpenRegionalTradeScenario Baseline { get; } = new();
}
