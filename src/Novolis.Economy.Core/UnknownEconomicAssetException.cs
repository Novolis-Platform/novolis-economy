namespace Novolis.Economy.Core;

/// <summary>Raised when an economic asset is missing in strict registration mode.</summary>
public sealed class UnknownEconomicAssetException(ResourceId id)
  : InvalidOperationException($"Unknown economic asset for resource {id}.");