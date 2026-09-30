namespace Novolis.Economy.Core;

/// <summary>Context required to turn a position quantity into monetary value.</summary>
public sealed record ValuationContext(
  EconomicAssetId UnitOfAccountAsset,
  int Period,
  ValuationMethod Method = ValuationMethod.PostedPrice);