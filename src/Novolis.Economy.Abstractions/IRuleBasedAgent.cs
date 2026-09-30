using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Model-neutral actor policy contract.</summary>
public interface IRuleBasedAgent<in TObservation, out TDecision>
    where TObservation : IAgentObservation
{
    AgentIdentity Identity { get; }

    TDecision Decide(TObservation observation, IAgentRandom random);
}
