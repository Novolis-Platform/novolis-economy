using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Named initial conditions and calibration for one model.</summary>
public interface IEconomicScenario
{
    string Id { get; }

    string Version { get; }
}
