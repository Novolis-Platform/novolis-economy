using Novolis.Economy.Core;
using Novolis.Economy.Core.Holdings;
using Novolis.Economy.Core.Steps;
using Novolis.Economy.Logistics;

namespace Novolis.Economy.Simulation;

/// <summary>
/// Converts logistics delivery events into Core economic position changes and
/// advances Core at period boundaries. Core remains the economic authority;
/// this integration does not maintain a second ownership balance.
/// </summary>
public static class CoreEconomyBridge
{
  private static readonly EconomyEngine PeriodEngine = DefaultPeriodPipeline.CreateEngine();

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

  /// <summary>Apply a delivered shipment into Core holdings at the destination hub's region.</summary>
  public static void ApplyDelivery(EconomyWorld world, ActiveShipment shipment)
  {
    if (shipment.IsLegacy && shipment.RouteId.Value == Guid.Empty)
    {
      return;
    }

    RegionId region;
    if (shipment.IsLegacy)
    {
      if (!world.Routes.TryGetValue(shipment.RouteId, out var route))
      {
        return;
      }

      // Legacy routes: use destination inventory location Guid as region key.
      region = RegionId.From(route.Destination.Value);
    }
    else
    {
      region = RegionForHub(world, shipment.CurrentHubId);
    }

    var owner = shipment.FirmId.AsCore();
    var resource = shipment.ProductId.AsCore();
    var qty = shipment.Quantity.Value;

    var state = world.CoreState;
    state = EnsureEntity(world, state, owner);
    state = EnsureRegion(world, state, region);
    state = EnsureResource(world, state, resource);
    state = HoldingLedger.Credit(state, owner, region, resource, qty);
    world.CoreState = state;
  }

  /// <summary>Run Core's 16-step period pipeline once.</summary>
  public static void AdvancePeriod(EconomyWorld world)
  {
    world.CoreState = PeriodEngine.Advance(world.CoreState);
  }

  private static EconomyState EnsureEntity(
    EconomyWorld world,
    EconomyState state,
    LegalEntityId id)
  {
    if (state.Entities.ContainsKey(id))
    {
      return state;
    }

    if (world.Registration != RegistrationMode.Implicit)
    {
      throw new UnknownEconomicEntityException(id);
    }

    var entities = new Dictionary<LegalEntityId, Core.LegalEntity>(state.Entities)
    {
      [id] = new Core.LegalEntity(id, Core.LegalEntityKind.Firm, Money.Zero),
    };
    return state with { Entities = entities };
  }

  private static EconomyState EnsureRegion(
    EconomyWorld world,
    EconomyState state,
    RegionId id)
  {
    if (state.Regions.ContainsKey(id))
    {
      return state;
    }

    if (world.Registration != RegistrationMode.Implicit)
    {
      throw new UnknownRegionException(id);
    }

    var regions = new Dictionary<RegionId, Region>(state.Regions)
    {
      [id] = new Region(id, LivingCapacity: 1_000_000, ProductionCapacity: 1_000_000, LogisticsCapacity: 1_000_000),
    };
    return state with { Regions = regions };
  }

  private static EconomyState EnsureResource(
    EconomyWorld world,
    EconomyState state,
    ResourceId id)
  {
    if (state.Resources.ContainsKey(id))
    {
      return state;
    }

    if (world.Registration != RegistrationMode.Implicit)
    {
      throw new UnknownEconomicAssetException(id);
    }

    var resources = new Dictionary<ResourceId, Resource>(state.Resources)
    {
      [id] = new Resource(
        id,
        Name: id.ToString(),
        Kind: ResourceKind.IntermediateGood,
        AssetId: EconomicAssetId.From(id.Value)),
    };
    return state with { Resources = resources };
  }
}

