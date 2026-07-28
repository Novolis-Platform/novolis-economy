using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Agents;

/// <summary>Cancel open hub orders for a firm (optional location/product filter).</summary>
public static class HubOrderQuotes
{
  /// <summary>Enqueue cancels for matching open orders.</summary>
  public static void CancelOpen(
    AgentContext context,
    FirmId firm,
    InventoryLocationId? location = null,
    ProductId? product = null,
    HubOrderSide? side = null)
  {
    foreach (var order in context.World.HubOrders
               .Where(o => o.FirmId.Equals(firm) && !o.IsFilled)
               .Where(o => location is null || o.LocationId.Equals(location.Value))
               .Where(o => product is null || o.ProductId.Equals(product.Value))
               .Where(o => side is null || o.Side == side.Value)
               .Select(o => o.Id)
               .ToList())
    {
      context.Enqueue(new CancelHubOrder(order));
    }
  }
}
