using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Logistics;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>Fluent builder for deterministic economy worlds.</summary>
public sealed class EconomyWorldBuilder
{
  private readonly EconomyWorld _world;
  private int _seq;

  /// <summary>Creates a builder.</summary>
  public EconomyWorldBuilder(EconomyPolicy? policy = null) =>
    _world = new EconomyWorld(policy);

  /// <summary>Registers a product definition.</summary>
  public EconomyWorldBuilder AddProduct(ProductDefinition product)
  {
    _world.Products[product.Id] = product;
    return this;
  }

  /// <summary>Registers a firm with opening cash.</summary>
  public EconomyWorldBuilder AddFirm(FirmId firmId, string name, Money openingCash)
  {
    var ledger = _world.EnsureFirm(firmId, name);
    if (openingCash.Amount > 0m)
    {
      ledger.SeedCash(openingCash, SimulationDate.Epoch);
    }

    return this;
  }

  /// <summary>Registers a civic entity (treasury) with opening cash and optional registry id.</summary>
  public EconomyWorldBuilder AddCivic(
    FirmId firmId,
    string name,
    Money openingCash,
    string? registryId = null)
  {
    var ledger = _world.EnsureFirm(firmId, name);
    _world.EnsureCivic(firmId, name, registryId);
    if (openingCash.Amount > 0m)
    {
      ledger.SeedCash(openingCash, SimulationDate.Epoch);
    }

    return this;
  }

  /// <summary>Registers a habitat/region with living and production caps.</summary>
  public EconomyWorldBuilder AddRegion(
    GeographicAreaId areaId,
    int livingCapacityHouseholds,
    int productionSlots)
  {
    _world.Regions[areaId] = new EconomicRegion(areaId, livingCapacityHouseholds, productionSlots);
    return this;
  }

  /// <summary>Sets an absolute ownership fraction (issuer must be Firm or Civic).</summary>
  public EconomyWorldBuilder SetOwnership(FirmId issuer, FirmId owner, decimal fraction)
  {
    OwnershipEngine.TryAssign(
      _world.OwnershipClaims, issuer, owner, fraction, _world.CanIssueShares);
    return this;
  }

  /// <summary>Registers a facility (mfg/assembly count against region production slots).</summary>
  public EconomyWorldBuilder AddFacility(FacilityBinding facility)
  {
    if (facility.Area is { } area
        && _world.Regions.TryGetValue(area, out var region)
        && EconomicRegion.ConsumesProductionSlot(facility.Layout)
        && _world.UsedProductionSlots(area) >= region.ProductionSlots)
    {
      return this;
    }

    _world.Facilities[facility.Id] = facility;
    _world.EnsureFirm(facility.FirmId, _world.Firms.GetValueOrDefault(facility.FirmId, facility.FirmId.ToString()));
    return this;
  }

  /// <summary>Adds opening inventory and books it on the ledger.</summary>
  public EconomyWorldBuilder AddInventory(
    FirmId firmId,
    InventoryLocationId location,
    ProductBatch batch)
  {
    _world.Inventory.Add(new InventoryKey(firmId, location, batch.ProductId), batch, bypassLimits: true);
    if (_world.Ledgers.TryGetValue(firmId, out var ledger))
    {
      ledger.SeedInventory(Money.From(batch.UnitCost.Amount * batch.Quantity.Value), batch.ProducedAt);
    }

    return this;
  }

  /// <summary>Adds a freight route.</summary>
  public EconomyWorldBuilder AddRoute(FreightRoute route)
  {
    _world.Routes[route.Id] = route;
    return this;
  }

  /// <summary>Adds a transport hub.</summary>
  public EconomyWorldBuilder AddHub(TransportHub hub)
  {
    _world.Hubs[hub.Id] = hub;
    CoreEconomyBridge.BindHubRegion(_world, hub.Id);
    return this;
  }

  /// <summary>Adds a transport hub bound to an explicit Core region.</summary>
  public EconomyWorldBuilder AddHub(TransportHub hub, RegionId regionId)
  {
    _world.Hubs[hub.Id] = hub;
    CoreEconomyBridge.BindHubRegion(_world, hub.Id, regionId);
    return this;
  }

  /// <summary>Adds a transport corridor.</summary>
  public EconomyWorldBuilder AddCorridor(TransportCorridor corridor)
  {
    _world.Corridors[corridor.Id] = corridor;
    return this;
  }

  /// <summary>Adds a vehicle class.</summary>
  public EconomyWorldBuilder AddVehicleClass(VehicleClass vehicle)
  {
    _world.VehicleClasses[vehicle.Id] = vehicle;
    return this;
  }

  /// <summary>Sets the world fuel product and write-off unit cost.</summary>
  public EconomyWorldBuilder SetTransportFuel(ProductId fuelProductId, Money unitCost)
  {
    _world.TransportFuelProductId = fuelProductId;
    _world.TransportFuelUnitCost = unitCost;
    return this;
  }

  /// <summary>Maps a facility to its restock route (storage → retail).</summary>
  public EconomyWorldBuilder SetRestockRoute(FacilityId facilityId, FreightRouteId routeId)
  {
    _world.RestockRoutes[facilityId] = routeId;
    return this;
  }

  /// <summary>Adds a consumer cohort as a household entity; clamps living capacity when region exists.</summary>
  public EconomyWorldBuilder AddCohort(ConsumerCohort cohort)
  {
    var households = cohort.Population;
    if (_world.Regions.TryGetValue(cohort.Area, out var region))
    {
      var used = _world.UsedLivingHouseholds(cohort.Area);
      var remaining = Math.Max(0, region.LivingCapacityHouseholds - used);
      var want = Population.HouseholdMath.Count(households);
      if (want > remaining)
      {
        households = new PopulationCount(remaining);
      }
    }

    if (Population.HouseholdMath.Count(households) <= 0)
    {
      return this;
    }

    var householdId = cohort.HouseholdFirmId ?? FirmId.From(NextGuid());
    _world.EnsureHousehold(householdId, $"Household {cohort.Id.Value.ToString("N")[..8]}");
    var linked = cohort with
    {
      Population = households,
      HouseholdFirmId = householdId,
    };
    _world.Cohorts.Add(new CohortState(linked));
    return this;
  }

  /// <summary>Sets available labor hours.</summary>
  public EconomyWorldBuilder SetLabor(FirmId firmId, decimal hoursPerTick)
  {
    _world.AvailableLaborHours[firmId] = hoursPerTick;
    return this;
  }

  /// <summary>Builds the world.</summary>
  public EconomyWorld Build() => _world;

  /// <summary>Next deterministic guid for scenarios.</summary>
  public Guid NextGuid()
  {
    _seq++;
    return Guid.Parse($"00000000-0000-4000-8000-{_seq:D12}");
  }
}
