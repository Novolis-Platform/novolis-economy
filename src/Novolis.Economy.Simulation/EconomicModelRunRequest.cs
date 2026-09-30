using System.Security.Cryptography;
using System.Text;
using Novolis.Economy.Abstractions;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Request for a generic run of a host-neutral model. The model owns one tick;
/// this type owns only the optional clock and run bookkeeping.
/// </summary>
public sealed record EconomicModelRunRequest(
    IEconomicModel Model,
    IEconomicScenario Scenario,
    ulong Seed,
    long Ticks,
    int PeriodLengthTicks = 24,
    IReadOnlyDictionary<long, IReadOnlyList<IEconomicModelCommand>>? Commands = null);
