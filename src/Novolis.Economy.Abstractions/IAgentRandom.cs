using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Named deterministic entropy supplied to an actor policy.</summary>
public interface IAgentRandom
{
    ulong State { get; }

    double NextDouble();

    int NextInt(int exclusiveUpperBound);
}
