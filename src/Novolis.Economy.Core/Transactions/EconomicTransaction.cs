using Novolis.Economy.Primitives;

namespace Novolis.Economy.Core.Transactions;

/// <summary>Atomic economic state transition.</summary>
public sealed record EconomicTransaction(
    TransactionId Id,
    IReadOnlyList<EconomicEffect> Effects,
    string? Reason = null);

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
