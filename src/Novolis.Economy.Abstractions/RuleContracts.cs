using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Stable identity for a replaceable economic rule.</summary>
public readonly record struct RuleIdentity(
    string Domain,
    string Name,
    string Version)
{
    public override string ToString() => $"{Domain}/{Name}@{Version}";
}

/// <summary>Model-neutral contract for a pure rule over a context.</summary>
public interface IEconomicRule<in TContext, out TResult>
{
    RuleIdentity Identity { get; }

    TResult Apply(TContext context);
}

/// <summary>Stable identity for a rules-based economic actor.</summary>
public readonly record struct AgentIdentity(
    EconomicEntityId Entity,
    string Role)
{
    public override string ToString() => $"{Role}/{Entity}";
}

/// <summary>Read-only observation supplied to a rules-based actor.</summary>
public interface IAgentObservation
{
    AgentIdentity Agent { get; }

    long Sequence { get; }
}

/// <summary>Model-neutral actor policy contract.</summary>
public interface IRuleBasedAgent<in TObservation, out TDecision>
    where TObservation : IAgentObservation
{
    AgentIdentity Identity { get; }

    TDecision Decide(TObservation observation, IAgentRandom random);
}

/// <summary>Named deterministic entropy supplied to an actor policy.</summary>
public interface IAgentRandom
{
    ulong State { get; }

    double NextDouble();

    int NextInt(int exclusiveUpperBound);
}
