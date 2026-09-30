namespace Novolis.Economy.Core;

/// <summary>Issued share class (SPEC §10).</summary>
public sealed record ShareClass(
  LegalEntityId Issuer,
  string Name,
  decimal IssuedUnits,
  decimal VotesPerUnit,
  decimal TreasuryUnits = 0m);