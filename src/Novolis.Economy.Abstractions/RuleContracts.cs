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
