using Novolis.Economy.Primitives;

namespace Novolis.Economy.Core.Transactions;

/// <summary>Atomic economic state transition.</summary>
public sealed record EconomicTransaction(
    TransactionId Id,
    IReadOnlyList<EconomicEffect> Effects,
    string? Reason = null,
    int? Period = null,
    string? Phase = null,
    EconomicEntityId? Actor = null)
{
    /// <summary>
    /// Creates a transaction identity from the Core run context. Setup APIs may
    /// still use <see cref="TransactionId.New"/>, but runtime transitions use
    /// this keyed allocator so inserting an unrelated transition does not
    /// consume ambient randomness.
    /// </summary>
    public static EconomicTransaction Create(
        EconomyState state,
        IReadOnlyList<EconomicEffect> effects,
        string? reason = null,
        string? phase = null,
        EconomicEntityId? actor = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(effects);

        var id = TransactionId.From(
            DeterministicIds.GuidFor(
                "economic-transaction",
                state.SimulationSeed,
                state.Period,
                state.TransitionSequence,
                reason ?? string.Empty,
                string.Join(";", effects.Select(effect => effect.ToString()))));
        return new EconomicTransaction(
            id,
            effects,
            reason,
            state.Period,
            phase,
            actor);
    }
}