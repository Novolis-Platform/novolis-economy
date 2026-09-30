namespace Novolis.Economy.Core;

/// <summary>Raised when an economic owner is missing in strict registration mode.</summary>
public sealed class UnknownEconomicEntityException(LegalEntityId id)
    : InvalidOperationException($"Unknown economic entity {id}.");