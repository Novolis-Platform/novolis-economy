namespace Novolis.Economy.Core;

/// <summary>Raised when an economic owner is missing in strict registration mode.</summary>
public sealed class UnknownEconomicEntityException(LegalEntityId id)
    : InvalidOperationException($"Unknown economic entity {id}.");

/// <summary>Raised when an economic asset is missing in strict registration mode.</summary>
public sealed class UnknownEconomicAssetException(ResourceId id)
    : InvalidOperationException($"Unknown economic asset for resource {id}.");

/// <summary>Raised when a spatial region is missing in strict registration mode.</summary>
public sealed class UnknownRegionException(RegionId id)
    : InvalidOperationException($"Unknown economic region {id}.");
