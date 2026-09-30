namespace Novolis.Economy.Core;

/// <summary>Raised when an economic asset identity is not registered.</summary>
public sealed class UnknownEconomicAssetIdException(EconomicAssetId id)
  : InvalidOperationException($"Unknown economic asset {id}.");