namespace Novolis.Economy.Core;

/// <summary>Posted unit price for matching (no order book; SPEC §20 / §22).</summary>
public sealed record PostedPrice(
  RegionId RegionId,
  ResourceId ResourceId,
  Money UnitPrice);