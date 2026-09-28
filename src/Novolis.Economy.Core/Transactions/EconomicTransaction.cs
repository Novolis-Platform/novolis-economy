using Novolis.Economy.Primitives;

namespace Novolis.Economy.Core.Transactions;

/// <summary>Atomic economic state transition.</summary>
public sealed record EconomicTransaction(
    TransactionId Id,
    IReadOnlyList<EconomicEffect> Effects,
    string? Reason = null)
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
        string? reason = null)
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
        return new EconomicTransaction(id, effects, reason);
    }
}

/// <summary>Base type for small, state-changing economic effects.</summary>
public abstract record EconomicEffect;

/// <summary>
/// Signed quantity change for one owner, asset, and optional region.
/// Stored positions remain non-negative; a transaction may contain a debit.
/// </summary>
public sealed record PositionChange(
    EconomicEntityId Owner,
    EconomicAssetId Asset,
    decimal Delta,
    RegionId? Region) : EconomicEffect;

/// <summary>Create or originate an authoritative financial claim.</summary>
public sealed record CreateClaim(FinancialClaim Claim) : EconomicEffect;

/// <summary>Reduce one claim by a quantity of its own denomination.</summary>
public sealed record SettleClaim(ClaimId ClaimId, AssetAmount Amount) : EconomicEffect;
