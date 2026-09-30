namespace Novolis.Economy.Core;

/// <summary>Who owns how much of which resource, and where (SPEC §8).</summary>
public sealed record ResourceHolding(
  LegalEntityId Owner,
  RegionId RegionId,
  ResourceId ResourceId,
  decimal Quantity);