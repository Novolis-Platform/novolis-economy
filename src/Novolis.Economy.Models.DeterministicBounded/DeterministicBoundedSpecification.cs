using Novolis.Economy.Abstractions;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Invariants;
using Novolis.Economy.Core.Production;
using Novolis.Economy.Core.Transactions;
using Novolis.Economy.Primitives;

namespace Novolis.Economy.Models.DeterministicBounded;

/// <summary>Finite parameters for exact replay and teaching.</summary>
public sealed record DeterministicBoundedSpecification(
    string Version = "deterministic-bounded-2",
    int HouseholdCount = 10,
    decimal InitialFood = 20m,
    decimal InitialCash = 500m,
    decimal HouseholdCash = 100m,
    decimal ProductionPerTick = 2m,
    decimal DemandPerHouseholdPerTick = 1m,
    decimal FoodPrice = 2m) : IEconomicModelSpecification;
