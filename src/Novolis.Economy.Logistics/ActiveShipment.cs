using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Mutable shipment (single-leg legacy or multi-leg itinerary).</summary>
public sealed class ActiveShipment
{
  /// <summary>Legacy single-leg constructor.</summary>
  public ActiveShipment(
    ShipmentId id,
    FirmId firmId,
    FreightRouteId routeId,
    ProductId productId,
    Quantity quantity,
    Money unitCost,
    long hoursRemaining,
    SimulationHour departedAt)
  {
    Id = id;
    FirmId = firmId;
    RouteId = routeId;
    ProductId = productId;
    Quantity = quantity;
    UnitCost = unitCost;
    HoursRemaining = hoursRemaining;
    DepartedAt = departedAt;
    Status = ShipmentStatus.InTransit;
    Phase = ShipmentPhase.Underway;
    Itinerary = Itinerary.Empty;
    LegIndex = 0;
    IsLegacy = true;
  }

  /// <summary>Multi-leg constructor.</summary>
  public ActiveShipment(
    ShipmentId id,
    FirmId firmId,
    ProductId productId,
    Quantity quantity,
    Money unitCost,
    SimulationHour departedAt,
    Itinerary itinerary,
    VehicleClass vehicle,
    TransportHubId originHubId,
    ProductId? fuelProductId)
  {
    Id = id;
    FirmId = firmId;
    RouteId = default;
    ProductId = productId;
    Quantity = quantity;
    UnitCost = unitCost;
    HoursRemaining = 0;
    DepartedAt = departedAt;
    Status = ShipmentStatus.InTransit;
    Phase = ShipmentPhase.Loading;
    Itinerary = itinerary;
    LegIndex = 0;
    Vehicle = vehicle;
    CurrentHubId = originHubId;
    FuelProductId = fuelProductId;
    OnboardFuel = Quantity.Zero;
    IsLegacy = false;
  }

  /// <summary>Shipment id.</summary>
  public ShipmentId Id { get; }

  /// <summary>Owning firm.</summary>
  public FirmId FirmId { get; }

  /// <summary>Legacy route id (default if multi-leg).</summary>
  public FreightRouteId RouteId { get; }

  /// <summary>Cargo product.</summary>
  public ProductId ProductId { get; }

  /// <summary>Cargo quantity.</summary>
  public Quantity Quantity { get; set; }

  /// <summary>Cargo unit cost.</summary>
  public Money UnitCost { get; }

  /// <summary>Legacy hours remaining on single leg.</summary>
  public long HoursRemaining { get; set; }

  /// <summary>Departure hour.</summary>
  public SimulationHour DepartedAt { get; }

  /// <summary>Legacy status.</summary>
  public ShipmentStatus Status { get; set; }

  /// <summary>Multi-leg phase.</summary>
  public ShipmentPhase Phase { get; set; }

  /// <summary>Planned corridors.</summary>
  public Itinerary Itinerary { get; }

  /// <summary>Current leg index (corridor about to enter or underway).</summary>
  public int LegIndex { get; set; }

  /// <summary>Vehicle profile (multi-leg).</summary>
  public VehicleClass? Vehicle { get; }

  /// <summary>Hub currently docked at (or last arrived).</summary>
  public TransportHubId CurrentHubId { get; set; }

  /// <summary>Fuel product when bunkering is enabled.</summary>
  public ProductId? FuelProductId { get; }

  /// <summary>Fuel currently onboard.</summary>
  public Quantity OnboardFuel { get; set; }

  /// <summary>Hours left in current dwell or transit segment.</summary>
  public long SegmentHoursRemaining { get; set; }

  /// <summary>Total hours for the current underway leg.</summary>
  public long LegHoursTotal { get; set; }

  /// <summary>Planned fuel burn for the current underway leg.</summary>
  public Quantity PlannedLegBurn { get; set; }

  /// <summary>True when created via FreightRoute shim.</summary>
  public bool IsLegacy { get; }

  /// <summary>Crew labor hours accrued this tick while underway.</summary>
  public decimal CrewLaborThisTick { get; set; }

  /// <summary>
  /// Consecutive hours stuck waiting to leave a hub (fuel stockout or unpaid toll).
  /// Reset when a leg begins; after <see cref="LogisticsEngine.MaxHubStallHours"/> the shipment
  /// unloads cargo at the current hub and cancels so the hull is not permanently locked.
  /// </summary>
  public int HubStallHours { get; set; }

  /// <summary>FTL/operating profile for this voyage (scales hours, fuel, and drive wear).</summary>
  public TransitProfile TransitProfile { get; set; } = TransitProfile.StandardCommercial;

  /// <summary>Drive wear accrued on this shipment while underway.</summary>
  public decimal DriveWearAccrued { get; set; }
}
