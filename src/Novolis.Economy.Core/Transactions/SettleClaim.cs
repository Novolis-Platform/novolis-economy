namespace Novolis.Economy.Core.Transactions;

/// <summary>Reduce one claim by a quantity of its own denomination.</summary>
public sealed record SettleClaim(ClaimId ClaimId, AssetAmount Amount) : EconomicEffect;