namespace Novolis.Economy.Core;

/// <summary>Named resource type (SPEC §7).</summary>
public sealed record Resource(
  ResourceId Id,
  string Name,
  ResourceKind Kind,
  EconomicAssetId AssetId);