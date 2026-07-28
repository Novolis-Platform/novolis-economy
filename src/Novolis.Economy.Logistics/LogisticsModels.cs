using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Transfer / bunker / berth location in the transport network.</summary>
/// <param name="Id">Hub id.</param>
/// <param name="LocationId">Inventory location for cargo and fuel at this hub.</param>
/// <param name="Name">Display name.</param>
/// <param name="DwellHours">Load/unload hours when calling at this hub.</param>
/// <param name="BerthCapacity">Shipments that can start dwell/leg per hour (0 = unlimited).</param>
public sealed record TransportHub(
  TransportHubId Id,
  InventoryLocationId LocationId,
  string Name,
  long DwellHours,
  int BerthCapacity);

/// <summary>Directed corridor between hubs.</summary>
/// <param name="Id">Corridor id.</param>
/// <param name="From">Origin hub.</param>
/// <param name="To">Destination hub.</param>
/// <param name="TransitHours">Hours underway.</param>
/// <param name="MaxCargo">Max cargo quantity on this leg.</param>
/// <param name="Difficulty">Unitless leg difficulty; fuel burn scales with TransitHours * Difficulty.</param>
/// <param name="Toll">Money toll charged on departure into this corridor.</param>
public sealed record TransportCorridor(
  TransportCorridorId Id,
  TransportHubId From,
  TransportHubId To,
  long TransitHours,
  Quantity MaxCargo,
  decimal Difficulty,
  Money Toll);

/// <summary>Thin vehicle capability profile.</summary>
/// <param name="Id">Class id.</param>
/// <param name="CargoCapacity">Max cargo quantity.</param>
/// <param name="FuelBurnPerDifficultyHour">Fuel units burned per (transitHour * difficulty).</param>
/// <param name="CrewLaborPerUnderwayHour">Labor hours accrued per underway hour.</param>
/// <param name="FuelTankCapacity">Max onboard fuel.</param>
public sealed record VehicleClass(
  VehicleClassId Id,
  Quantity CargoCapacity,
  decimal FuelBurnPerDifficultyHour,
  decimal CrewLaborPerUnderwayHour,
  Quantity FuelTankCapacity);

/// <summary>Ordered path through corridors.</summary>
/// <param name="CorridorIds">Corridor sequence.</param>
public sealed record Itinerary(ImmutableArray<TransportCorridorId> CorridorIds)
{
  /// <summary>Empty itinerary.</summary>
  public static Itinerary Empty { get; } = new(ImmutableArray<TransportCorridorId>.Empty);

  /// <summary>Number of legs.</summary>
  public int LegCount => CorridorIds.Length;
}

/// <summary>Compat shim: single-edge freight route (maps to one corridor conceptually).</summary>
public sealed record FreightRoute(
  FreightRouteId Id,
  InventoryLocationId Origin,
  InventoryLocationId Destination,
  long TransitHours,
  Quantity Capacity);

/// <summary>Legacy shipment status.</summary>
public enum ShipmentStatus
{
  /// <summary>Queued.</summary>
  Queued = 0,
  /// <summary>In transit (legacy single-leg or multi-leg underway).</summary>
  InTransit = 1,
  /// <summary>Delivered.</summary>
  Delivered = 2,
  /// <summary>Cancelled.</summary>
  Cancelled = 3,
}

/// <summary>Phase of a multi-leg shipment.</summary>
public enum ShipmentPhase
{
  /// <summary>Loading at origin hub.</summary>
  Loading = 0,
  /// <summary>Moving along a corridor.</summary>
  Underway = 1,
  /// <summary>Unloading / bunkering at an intermediate or final hub.</summary>
  Unloading = 2,
  /// <summary>Waiting for a berth.</summary>
  WaitingBerth = 3,
  /// <summary>Complete.</summary>
  Delivered = 4,
  /// <summary>Failed / cancelled.</summary>
  Cancelled = 5,
}

/// <summary>Immutable snapshot (legacy).</summary>
public sealed record Shipment(
  ShipmentId Id,
  FreightRouteId RouteId,
  ProductId ProductId,
  Quantity Quantity,
  SimulationHour DepartedAt,
  SimulationHour ExpectedArrival,
  ShipmentStatus Status);

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
  public Quantity Quantity { get; }

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
}
