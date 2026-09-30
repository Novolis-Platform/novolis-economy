using System.Collections.Immutable;
using Novolis.Economy;

namespace Novolis.Economy.Logistics;

/// <summary>Compat shim: single-edge freight route (maps to one corridor conceptually).</summary>
public sealed record FreightRoute(
  FreightRouteId Id,
  InventoryLocationId Origin,
  InventoryLocationId Destination,
  long TransitHours,
  Quantity Capacity);
