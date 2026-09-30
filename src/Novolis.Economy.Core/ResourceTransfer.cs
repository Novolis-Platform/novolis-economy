namespace Novolis.Economy.Core;

/// <summary>In-flight movement preserving ownership unless a sale occurs (SPEC §9).</summary>
public sealed record ResourceTransfer(
  LegalEntityId Owner,
  ResourceId ResourceId,
  decimal Quantity,
  RegionId Origin,
  RegionId Destination,
  int RemainingPeriods);