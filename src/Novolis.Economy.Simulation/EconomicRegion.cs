using Novolis.Economy;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Habitat/region at economic resolution: living + production caps for one
/// <see cref="GeographicAreaId"/> (homogeneous blob; hub logistics at this grain).
/// </summary>
public sealed class EconomicRegion
{
  /// <summary>Creates a region.</summary>
  public EconomicRegion(
    GeographicAreaId areaId,
    int livingCapacityHouseholds,
    int productionSlots)
  {
    AreaId = areaId;
    LivingCapacityHouseholds = Math.Max(0, livingCapacityHouseholds);
    ProductionSlots = Math.Max(0, productionSlots);
  }

  /// <summary>Region / habitat id.</summary>
  public GeographicAreaId AreaId { get; }

  /// <summary>Max household count (from headcount / people-per-household).</summary>
  public int LivingCapacityHouseholds { get; set; }

  /// <summary>Max facilities with manufacturing or assembly units.</summary>
  public int ProductionSlots { get; set; }

  /// <summary>Whether a facility layout consumes a production slot.</summary>
  public static bool ConsumesProductionSlot(FacilityLayout layout) =>
    layout.Units.Values.Any(u =>
      u.Kind is OperatingUnitKind.Manufacturing or OperatingUnitKind.Assembly);
}
