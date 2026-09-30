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
