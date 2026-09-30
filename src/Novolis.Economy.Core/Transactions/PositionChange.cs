namespace Novolis.Economy.Core.Transactions;

/// <summary>
/// Signed quantity change for one owner, asset, and optional region.
/// Stored positions remain non-negative; a transaction may contain a debit.
/// </summary>
public sealed record PositionChange(
  EconomicEntityId Owner,
  EconomicAssetId Asset,
  decimal Delta,
  RegionId? Region) : EconomicEffect;