using Novolis.Economy.Core;
using Novolis.Economy.Logistics;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Composes Core with the operational world by binding spatial identities and
/// advancing Core at period boundaries. Stock synchronization lives in the
/// explicitly named inventory and cash projection adapters; this type is not
/// an economic stock of record.
/// </summary>
public static class CoreEconomyBridge
{
  /// <summary>Maps a hub to a Core region (default: same Guid as hub id).</summary>
  public static RegionId RegionForHub(EconomyWorld world, TransportHubId hubId)
  {
    if (world.HubRegions.TryGetValue(hubId, out var region))
    {
      return region;
    }

    return RegionId.From(hubId.Value);
  }

  /// <summary>Register hub ↔ region; creates a Core region stub if missing.</summary>
  public static void BindHubRegion(EconomyWorld world, TransportHubId hubId, RegionId? regionId = null)
  {
    var region = regionId ?? RegionId.From(hubId.Value);
    world.HubRegions[hubId] = region;
    if (world.Hubs.TryGetValue(hubId, out var hub))
      world.InventoryLocationRegions[hub.LocationId] = region;
    if (!world.CoreState.Regions.ContainsKey(region))
    {
      var regions = new Dictionary<RegionId, Region>(world.CoreState.Regions)
      {
        [region] = new Region(
          region,
          LivingCapacity: 1_000_000,
          ProductionCapacity: 1_000_000,
          LogisticsCapacity: 1_000_000)
      };
      world.CoreState = world.CoreState with { Regions = regions };
    }
  }

}

