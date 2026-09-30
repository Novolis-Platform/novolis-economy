using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Core.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>Facility binding to firm and inventory locations.</summary>
public sealed class FacilityBinding
{
  /// <summary>Creates a facility binding.</summary>
  public FacilityBinding(
    FacilityId id,
    FirmId firmId,
    InventoryLocationId storageLocation,
    InventoryLocationId? retailLocation,
    FacilityLayout layout,
    GeographicAreaId? area = null)
  {
    Id = id;
    FirmId = firmId;
    StorageLocation = storageLocation;
    RetailLocation = retailLocation;
    Layout = layout;
    Area = area;
  }

  /// <summary>Facility id.</summary>
  public FacilityId Id { get; }

  /// <summary>Owning firm.</summary>
  public FirmId FirmId { get; }

  /// <summary>Primary storage location.</summary>
  public InventoryLocationId StorageLocation { get; }

  /// <summary>Optional retail shelf location.</summary>
  public InventoryLocationId? RetailLocation { get; }

  /// <summary>Layout graph.</summary>
  public FacilityLayout Layout { get; }

  /// <summary>
  /// Optional geographic area for local demand. Null = visible to all cohorts
  /// (legacy / global retail).
  /// </summary>
  public GeographicAreaId? Area { get; }

  /// <summary>Manufacturing capacity summed from manufacturing units.</summary>
  public Quantity ManufacturingCapacity =>
    Quantity.From(
      Layout.Units.Values
        .Where(u => u.Kind is OperatingUnitKind.Manufacturing or OperatingUnitKind.Assembly)
        .Sum(u => u.Capacity.Value));
}
