using Novolis.Economy;
using Novolis.Economy.Production;

namespace Novolis.Economy.Logistics;

/// <summary>Advances and creates shipments.</summary>
public static class LogisticsEngine
{
  /// <summary>Pulls stock and creates an in-transit shipment.</summary>
  public static ActiveShipment? TryDepart(
    InventoryStore inventory,
    FirmId firmId,
    FreightRoute route,
    ProductId productId,
    Quantity quantity,
    SimulationHour now,
    out Money unitCost)
  {
    unitCost = Money.Zero;
    var key = new InventoryKey(firmId, route.Origin, productId);
    if (!inventory.TryTake(key, quantity, out _, out var totalCost) || quantity.Value <= 0m)
    {
      return null;
    }

    unitCost = Money.From(totalCost.Amount / quantity.Value);
    return new ActiveShipment(
      ShipmentId.From(CreateShipmentGuid(firmId, now, productId, quantity)),
      firmId,
      route.Id,
      productId,
      quantity,
      unitCost,
      Math.Max(1, route.TransitHours),
      now);
  }

  /// <summary>Ticks all shipments one hour; returns deliveries.</summary>
  public static List<ActiveShipment> AdvanceHour(
    IList<ActiveShipment> shipments,
    InventoryStore inventory,
    IReadOnlyDictionary<FreightRouteId, FreightRoute> routes)
  {
    var delivered = new List<ActiveShipment>();
    foreach (var shipment in shipments.Where(s => s.Status == ShipmentStatus.InTransit).OrderBy(s => s.Id.Value))
    {
      shipment.HoursRemaining--;
      if (shipment.HoursRemaining > 0)
      {
        continue;
      }

      if (!routes.TryGetValue(shipment.RouteId, out var route))
      {
        shipment.Status = ShipmentStatus.Cancelled;
        continue;
      }

      inventory.Add(
        new InventoryKey(shipment.FirmId, route.Destination, shipment.ProductId),
        new ProductBatch(
          shipment.ProductId,
          shipment.Quantity,
          new ProductQuality(100m),
          shipment.UnitCost,
          shipment.DepartedAt.Date,
          BrandId: null));
      shipment.Status = ShipmentStatus.Delivered;
      delivered.Add(shipment);
    }

    return delivered;
  }

  private static Guid CreateShipmentGuid(FirmId firmId, SimulationHour now, ProductId productId, Quantity qty)
  {
    var bytes = firmId.Value.ToByteArray();
    var hour = BitConverter.GetBytes(now.HourIndex);
    Buffer.BlockCopy(hour, 0, bytes, 8, 4);
    bytes[12] = (byte)productId.Value.GetHashCode();
    bytes[13] = (byte)qty.Value;
    bytes[14] = 0x51;
    bytes[15] = 0x1F;
    return new Guid(bytes);
  }
}
