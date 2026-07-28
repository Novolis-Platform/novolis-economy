using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Defined path between inventory locations.</summary>
/// <param name="Id">Route id.</param>
/// <param name="Origin">Origin location.</param>
/// <param name="Destination">Destination location.</param>
/// <param name="TransitHours">Nominal transit duration in hours.</param>
/// <param name="Capacity">Capacity stub in quantity units.</param>
public sealed record FreightRoute(
  FreightRouteId Id,
  InventoryLocationId Origin,
  InventoryLocationId Destination,
  long TransitHours,
  Quantity Capacity);

/// <summary>Shipment status.</summary>
public enum ShipmentStatus
{
  /// <summary>Queued for dispatch.</summary>
  Queued = 0,
  /// <summary>In transit.</summary>
  InTransit = 1,
  /// <summary>Delivered.</summary>
  Delivered = 2,
  /// <summary>Cancelled.</summary>
  Cancelled = 3,
}

/// <summary>Physical goods movement between locations (immutable snapshot).</summary>
public sealed record Shipment(
  ShipmentId Id,
  FreightRouteId RouteId,
  ProductId ProductId,
  Quantity Quantity,
  SimulationHour DepartedAt,
  SimulationHour ExpectedArrival,
  ShipmentStatus Status);

/// <summary>Mutable in-flight shipment used by the logistics engine.</summary>
public sealed class ActiveShipment
{
  /// <summary>Creates an active shipment.</summary>
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
  }

  /// <summary>Shipment id.</summary>
  public ShipmentId Id { get; }

  /// <summary>Owning firm.</summary>
  public FirmId FirmId { get; }

  /// <summary>Route.</summary>
  public FreightRouteId RouteId { get; }

  /// <summary>Product.</summary>
  public ProductId ProductId { get; }

  /// <summary>Quantity.</summary>
  public Quantity Quantity { get; }

  /// <summary>Carrying unit cost.</summary>
  public Money UnitCost { get; }

  /// <summary>Hours left until delivery.</summary>
  public long HoursRemaining { get; set; }

  /// <summary>Departure hour.</summary>
  public SimulationHour DepartedAt { get; }

  /// <summary>Status.</summary>
  public ShipmentStatus Status { get; set; }
}
