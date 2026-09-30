using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Read-only observation supplied to a rules-based actor.</summary>
public interface IAgentObservation
{
    AgentIdentity Agent { get; }

    long Sequence { get; }
}
