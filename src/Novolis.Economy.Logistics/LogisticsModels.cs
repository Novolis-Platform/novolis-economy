using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Identifies an inventory location (warehouse bin, vehicle, store shelf).</summary>
/// <param name="Value">Opaque location key.</param>
public readonly record struct InventoryLocationId(Guid Value)
{
  /// <summary>Creates a new location id.</summary>
  public static InventoryLocationId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a shipment in transit.</summary>
/// <param name="Value">Opaque shipment key.</param>
public readonly record struct ShipmentId(Guid Value)
{
  /// <summary>Creates a new shipment id.</summary>
  public static ShipmentId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

/// <summary>Identifies a freight route.</summary>
/// <param name="Value">Opaque route key.</param>
public readonly record struct FreightRouteId(Guid Value)
{
  /// <summary>Creates a new route id.</summary>
  public static FreightRouteId New() => new(Guid.NewGuid());

  /// <inheritdoc />
  public override string ToString() => Value.ToString("N");
}

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

/// <summary>Physical goods movement between locations.</summary>
/// <param name="Id">Shipment id.</param>
/// <param name="RouteId">Route used.</param>
/// <param name="ProductId">Product moved.</param>
/// <param name="Quantity">Quantity moved.</param>
/// <param name="DepartedAt">Departure hour.</param>
/// <param name="ExpectedArrival">Expected arrival hour.</param>
/// <param name="Status">Current status.</param>
public sealed record Shipment(
  ShipmentId Id,
  FreightRouteId RouteId,
  ProductId ProductId,
  Quantity Quantity,
  SimulationHour DepartedAt,
  SimulationHour ExpectedArrival,
  ShipmentStatus Status);
