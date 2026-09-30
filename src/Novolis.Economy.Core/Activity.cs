namespace Novolis.Economy.Core;

/// <summary>Productive unit operated by a firm in a region (SPEC §6).</summary>
public sealed record Activity(
  ActivityId Id,
  LegalEntityId Operator,
  RegionId RegionId,
  ActivityRecipe Recipe,
  decimal InstalledCapacity);