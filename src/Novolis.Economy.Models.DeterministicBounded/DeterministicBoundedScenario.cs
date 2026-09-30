using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Models.DeterministicBounded;

/// <summary>Fixed initial conditions for the bounded model.</summary>
public sealed record DeterministicBoundedScenario(
    string Id = "finite-one-region",
    string Version = "deterministic-bounded-scenario-1") : IEconomicScenario
{
    public static DeterministicBoundedScenario Baseline { get; } = new();
}
