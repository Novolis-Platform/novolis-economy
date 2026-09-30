namespace Novolis.Economy.Core;

/// <summary>Party that may own assets, owe obligations, and transact (SPEC §2).</summary>
public sealed record LegalEntity(
    LegalEntityId Id,
    LegalEntityKind Kind,
    Money Cash);