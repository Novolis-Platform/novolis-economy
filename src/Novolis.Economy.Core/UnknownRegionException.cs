namespace Novolis.Economy.Core;

/// <summary>Raised when a spatial region is missing in strict registration mode.</summary>
public sealed class UnknownRegionException(RegionId id)
  : InvalidOperationException($"Unknown economic region {id}.");