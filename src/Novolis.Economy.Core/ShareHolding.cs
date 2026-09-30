namespace Novolis.Economy.Core;

/// <summary>Units of a named share class held by an owner (SPEC §10).</summary>
public sealed record ShareHolding(
  LegalEntityId Owner,
  LegalEntityId Issuer,
  string ShareClass,
  decimal Units);