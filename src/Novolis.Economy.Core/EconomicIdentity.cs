namespace Novolis.Economy.Core;

/// <summary>
/// Explicit adapters between the legacy Core legal-entity id and the
/// package-neutral economic owner id.
/// </summary>
public static class EconomicIdentity
{
    /// <summary>Map a Core legal entity into the economic vocabulary.</summary>
    public static EconomicEntityId For(LegalEntityId id) => EconomicEntityId.From(id.Value);

    /// <summary>Map an economic owner into the legacy Core entity id.</summary>
    public static LegalEntityId ToLegalEntityId(EconomicEntityId id) => LegalEntityId.From(id.Value);
}
