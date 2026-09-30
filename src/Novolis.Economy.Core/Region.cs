namespace Novolis.Economy.Core;

/// <summary>Homogeneous economic point (SPEC §3).</summary>
public sealed record Region(
  RegionId Id,
  int LivingCapacity,
  decimal ProductionCapacity,
  decimal LogisticsCapacity);