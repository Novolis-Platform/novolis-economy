namespace Novolis.Economy.Core.Extensions;

/// <summary>Capacity and demographic insight for one region.</summary>
public sealed record RegionInsight(
  RegionId Id,
  int LivingCapacity,
  int Households,
  int RemainingLiving,
  decimal LivingUtilization,
  decimal ProductionCapacity,
  decimal InstalledProductionSpace,
  decimal RemainingProduction,
  decimal ProductionUtilization,
  decimal LogisticsCapacity,
  decimal LogisticsLoad,
  decimal RemainingLogistics,
  decimal LogisticsUtilization,
  decimal LaborSupplyHours,
  int ActivityCount,
  decimal HoldingQuantity);