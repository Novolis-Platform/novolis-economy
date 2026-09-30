using Novolis.Economy.Primitives;

namespace Novolis.Economy.Abstractions;

/// <summary>Stable identity for a rules-based economic actor.</summary>
public readonly record struct AgentIdentity(
    EconomicEntityId Entity,
    string Role)
{
    public override string ToString() => $"{Role}/{Entity}";
}
