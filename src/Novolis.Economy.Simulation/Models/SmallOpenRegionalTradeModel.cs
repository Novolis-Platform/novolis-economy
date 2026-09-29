using System.Collections.Immutable;
using Novolis.Economy.Agents;
using Novolis.Economy.Core;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Models;

/// <summary>Serializable assumptions for the flagship small open model.</summary>
public sealed record SmallOpenRegionalTradeSpecification(
  string Version = "small-open-regional-trade-1",
  int RegionCount = 3,
  decimal WorldPrice = 10m,
  decimal ImportCapacityPerPeriod = 100m,
  decimal ExportDemandPerPeriod = 100m,
  bool EnableRuleBasedAgents = true,
  bool EnableDeterministicFuzzing = false,
  MonetaryClosure Closure = MonetaryClosure.ExternalSector,
  decimal ExternalOpeningCash = 100_000m,
  int HouseholdCount = 120,
  decimal FoodProductionPerHour = 12m,
  decimal CreditLimit = 10_000m,
  decimal CreditInterestRatePerPeriod = 0.01m,
  int CreditTermPeriods = 4);

/// <summary>
/// Flagship model composition: regional production, households, trade,
/// logistics, credit, external closure, and optional rules-based actors.
/// </summary>
public sealed class SmallOpenRegionalTradeModel : SimulationModelDefinition
{
  private static readonly Guid North = Guid.Parse("1d1e1f20-0000-4000-8000-000000000001");
  private static readonly Guid Central = Guid.Parse("1d1e1f20-0000-4000-8000-000000000002");
  private static readonly Guid South = Guid.Parse("1d1e1f20-0000-4000-8000-000000000003");
  private static readonly Guid Food = Guid.Parse("2d2e2f30-0000-4000-8000-000000000001");
  private static readonly Guid Fuel = Guid.Parse("2d2e2f30-0000-4000-8000-000000000002");
  private static readonly Guid Machinery = Guid.Parse("2d2e2f30-0000-4000-8000-000000000003");
  private static readonly Guid Household = Guid.Parse("3d3e3f40-0000-4000-8000-000000000001");
  private static readonly Guid Producer = Guid.Parse("3d3e3f40-0000-4000-8000-000000000002");
  private static readonly Guid Carrier = Guid.Parse("3d3e3f40-0000-4000-8000-000000000003");
  private static readonly Guid Bank = Guid.Parse("3d3e3f40-0000-4000-8000-000000000004");
  private static readonly Guid External = Guid.Parse("3d3e3f40-0000-4000-8000-000000000005");
  private static readonly Guid WorkingCapital = Guid.Parse(
    "4d4e4f50-0000-4000-8000-000000000001");

  /// <summary>Creates the flagship model with default assumptions.</summary>
  public SmallOpenRegionalTradeModel(
    SmallOpenRegionalTradeSpecification? specification = null)
  {
    Trade = specification ?? new SmallOpenRegionalTradeSpecification();
  }

  /// <summary>Scenario-level assumptions for external trade and agents.</summary>
  public SmallOpenRegionalTradeSpecification Trade { get; }

  /// <inheritdoc />
  public override SimulationModelIdentity Identity =>
    new("SmallOpenRegionalTrade", Trade.Version);

  /// <inheritdoc />
  public override EconomyModelSpecification Specification =>
    EconomyModelSpecification.Default with
    {
      Version = Trade.Version,
      Credit = new CreditSpecification(
        Trade.CreditInterestRatePerPeriod,
        Trade.CreditTermPeriods,
        EconomyModelSpecification.Default.Credit.DelinquencyPeriodsBeforeDefault)
    };

  /// <inheritdoc />
  public override IReadOnlyList<string> RuleIdentities =>
  [
    "production.capacity-constrained",
    "population.cohort-budget-share",
    "markets.posted-price-rationing",
    "finance.working-capital-credit",
    "logistics.time-and-capacity-constrained",
    "closure.explicit-external-sector"
  ];

  /// <inheritdoc />
  public override IReadOnlyList<string> AgentProfileIds =>
    Trade.EnableRuleBasedAgents
      ? ["firms.rule-based", "carriers.rule-based"]
      : ["agents.disabled"];

  /// <inheritdoc />
  public override object ReproducibilityDescriptor => Trade;

  /// <inheritdoc />
  public override EconomyWorld CreateWorld(ulong seed)
  {
    var builder = new EconomyWorldBuilder(
      policy: new EconomyPolicy
      {
        PeriodHours = SimulationHour.HoursPerDay,
        PriceElasticity = 0.8m,
        HouseholdCreditFromWages = true,
        CohortBudgetResetMode = CohortBudgetResetMode.CarryForward
      },
      registrationMode: RegistrationMode.Strict,
      modelSpecification: Specification);

    AddProducts(builder);
    AddRegions(builder);
    AddFirms(builder);
    AddHousehold(builder);
    var world = builder.Build();
    world.MonetaryClosure = Trade.Closure;
    AddWorkingCapitalFacility(world);

    // Explicit external-sector flows: import fuel and export food. These are
    // settled against the registered external ledger in AcquireInputsPhase.
    var northStorage = InventoryLocationId.From(GuidUtility(North, 40));
    world.PendingProcurement.Add(new PlaceProcurementOrder(
      FirmId.From(Producer),
      northStorage,
      ProductId.From(Fuel),
      Quantity.From(Trade.ImportCapacityPerPeriod),
      Money.From(Trade.WorldPrice)));
    world.PendingExports.Add(new PlaceExportOrder(
      FirmId.From(Producer),
      northStorage,
      ProductId.From(Food),
      Quantity.From(Trade.ExportDemandPerPeriod),
      Money.From(Trade.WorldPrice)));

    return world;
  }

  private void AddWorkingCapitalFacility(EconomyWorld world)
  {
    if (Trade.CreditLimit <= 0m)
      return;

    var facilityId = CreditFacilityId.From(WorkingCapital);
    var facilities = new Dictionary<CreditFacilityId, CreditFacility>(
      world.CoreState.CreditFacilities)
    {
      [facilityId] = new CreditFacility(
        facilityId,
        FirmId.From(Bank).AsCore(),
        FirmId.From(Producer).AsCore(),
        Money.From(Trade.CreditLimit),
        Money.Zero,
        IsCommitted: true)
    };
    world.CoreState = world.CoreState with { CreditFacilities = facilities };
  }

  private static void AddProducts(EconomyWorldBuilder builder)
  {
    AddProduct(builder, Food, "Food");
    AddProduct(builder, Fuel, "Fuel");
    AddProduct(builder, Machinery, "Machinery");
  }

  private static void AddProduct(EconomyWorldBuilder builder, Guid id, string name)
  {
    var product = ProductId.From(id);
    var inputs = name == "Machinery"
      ? ImmutableArray.Create(new ProductInput(
          ProductId.From(Fuel),
          Quantity.From(0.25m)))
      : ImmutableArray<ProductInput>.Empty;
    builder.AddProduct(new ProductDefinition(
      product,
      ProductCategoryId.From(GuidUtility(id, 1)),
      inputs,
      ImmutableArray<ProductAttributeDefinition>.Empty,
      ProductionProcessId.From(GuidUtility(id, 2)),
      ShelfLife: null));
  }

  private void AddRegions(EconomyWorldBuilder builder)
  {
    var capacity = Math.Clamp(Trade.RegionCount, 1, 3);
    var regions = new[] { North, Central, South }.Take(capacity);
    foreach (var region in regions)
    {
      builder.AddRegion(GeographicAreaId.From(region), 2_000, 100);
    }
  }

  private void AddFirms(EconomyWorldBuilder builder)
  {
    builder.AddFirm(FirmId.From(Producer), "Regional Producer", Money.From(5_000m));
    builder.AddFirm(FirmId.From(Carrier), "Regional Carrier", Money.From(3_000m));
    builder.AddBank(FirmId.From(Bank), "Regional Bank", Money.From(25_000m));
    builder.AddExternalSector(
      FirmId.From(External),
      "External Sector",
      Money.From(Trade.ExternalOpeningCash));

    var regions = new[] { North, Central, South };
    foreach (var region in regions)
    {
      var area = GeographicAreaId.From(region);
      var storage = InventoryLocationId.From(GuidUtility(region, 40));
      var retail = InventoryLocationId.From(GuidUtility(region, 41));
      var facility = FacilityId.From(GuidUtility(region, 42));
      var unit = OperatingUnitId.From(GuidUtility(region, 43));
      var sales = OperatingUnitId.From(GuidUtility(region, 44));
      var layout = new FacilityLayout(
        ImmutableDictionary<OperatingUnitId, OperatingUnit>.Empty
          .Add(unit, new OperatingUnit(
            unit,
            OperatingUnitKind.Manufacturing,
            Quantity.From(TradeCapacity(region))))
          .Add(sales, new OperatingUnit(
            sales,
            OperatingUnitKind.Sales,
            Quantity.From(TradeCapacity(region)))),
        ImmutableArray<MaterialRoute>.Empty);
      builder.AddFacility(new FacilityBinding(
        facility,
        FirmId.From(Producer),
        storage,
        retail,
        layout,
        area));
      builder.SetProductionPlan(
        FirmId.From(Producer),
        facility,
        ProductId.From(Food),
        Quantity.From(TradeCapacity(region)));
      builder.SetRetailPrice(
        FirmId.From(Producer),
        facility,
        ProductId.From(Food),
        Money.From(10m));
      builder.AddInventory(
        FirmId.From(Producer),
        storage,
        new ProductBatch(
          ProductId.From(Fuel),
          Quantity.From(50m),
          new ProductQuality(100m),
          Money.From(2m),
          SimulationDate.Epoch,
          null));
      if (region == North)
      {
        builder.AddInventory(
          FirmId.From(Producer),
          storage,
          new ProductBatch(
            ProductId.From(Food),
            Quantity.From(80m),
            new ProductQuality(100m),
            Money.From(4m),
            SimulationDate.Epoch,
            null));
      }
    }

    AddTransport(builder);
  }

  private void AddHousehold(EconomyWorldBuilder builder)
  {
    builder.AddCohort(new ConsumerCohort(
      ConsumerCohortId.From(Household),
      new PopulationCount(Trade.HouseholdCount),
      Money.From(100m),
      new PreferenceProfile(
        ImmutableArray<CategoryPreference>.Empty,
        PriceSensitivity: 0.8m,
        QualitySensitivity: 0m,
        BrandLoyalty: 0m),
      GeographicAreaId.From(Central),
      HouseholdProductivityKind.Mean,
      FirmId.From(Household)));
  }

  private static void AddTransport(EconomyWorldBuilder builder)
  {
    var northLocation = InventoryLocationId.From(GuidUtility(North, 40));
    var centralLocation = InventoryLocationId.From(GuidUtility(Central, 40));
    var southLocation = InventoryLocationId.From(GuidUtility(South, 40));
    var northHub = TransportHubId.From(GuidUtility(North, 50));
    var centralHub = TransportHubId.From(GuidUtility(Central, 50));
    var southHub = TransportHubId.From(GuidUtility(South, 50));
    builder
      .AddHub(new TransportHub(northHub, northLocation, "North", 1, 2), RegionId.From(North))
      .AddHub(new TransportHub(centralHub, centralLocation, "Central", 1, 2), RegionId.From(Central))
      .AddHub(new TransportHub(southHub, southLocation, "South", 1, 2), RegionId.From(South));

    AddCorridor(builder, northHub, centralHub, GuidUtility(North, 51));
    AddCorridor(builder, centralHub, northHub, GuidUtility(North, 52));
    AddCorridor(builder, centralHub, southHub, GuidUtility(South, 51));
    AddCorridor(builder, southHub, centralHub, GuidUtility(South, 52));

    var vehicle = new VehicleClass(
      VehicleClassId.From(GuidUtility(Carrier, 50)),
      Quantity.From(40m),
      FuelBurnPerDifficultyHour: 0.5m,
      CrewLaborPerUnderwayHour: 1m,
      FuelTankCapacity: Quantity.From(40m));
    builder.AddVehicleClass(vehicle);
    builder.SetTransportFuel(ProductId.From(Fuel), Money.From(2m));
  }

  private static void AddCorridor(
    EconomyWorldBuilder builder,
    TransportHubId origin,
    TransportHubId destination,
    Guid id) =>
    builder.AddCorridor(new TransportCorridor(
      TransportCorridorId.From(id),
      origin,
      destination,
      TransitHours: 8,
      MaxCargo: Quantity.From(40m),
      Difficulty: 1m,
      Toll: Money.From(5m)));

  private decimal TradeCapacity(Guid region) =>
    Trade.FoodProductionPerHour * (region == Central ? 1.5m : 1m);

  private static Guid GuidUtility(Guid source, int salt)
  {
    var bytes = source.ToByteArray();
    bytes[0] ^= (byte)salt;
    bytes[1] ^= (byte)(salt * 17);
    return new Guid(bytes);
  }

  /// <inheritdoc />
  public override IReadOnlyList<IEconomicAgent> CreateAgents(
    EconomyWorld world,
    ulong seed)
  {
    if (!Trade.EnableRuleBasedAgents)
      return Array.Empty<IEconomicAgent>();

    var producerSites = world.Facilities.Values
      .Where(f => f.FirmId == FirmId.From(Producer) && f.RetailLocation is not null)
      .Select(f => new AgentSite(
        f.StorageLocation,
        f.Id,
        world.HubRegions.FirstOrDefault(h =>
          h.Value == f.Area?.AsCore()).Key,
        world.Firms.GetValueOrDefault(f.FirmId, "producer")))
      .ToArray();
    var vehicle = world.VehicleClasses[VehicleClassId.From(GuidUtility(Carrier, 50))];
    var carrierSites = world.Hubs.Values
      .Select(h => new AgentSite(h.LocationId, HubId: h.Id, Name: h.Name))
      .ToArray();

    return
    [
      new RetailFirmAgent(
        FirmId.From(Producer),
        new RetailFirmAgentPolicy(
          producerSites,
          producerSites,
          [new RetailSkuPolicy(
            ProductId.From(Food),
            Trade.WorldPrice,
            StockTarget: 20m,
            DeliveredLimitPrice: Trade.WorldPrice,
            PostRetailPrice: true)],
          new BunkerSkuPolicy(
            ProductId.From(Fuel),
            MinStock: 10m,
            BuyLimitPrice: Trade.WorldPrice,
            SellPrice: Trade.WorldPrice,
            AllowProcurement: true))),
      new CarrierFirmAgent(
        FirmId.From(Carrier),
        new CarrierFirmAgentPolicy(
          carrierSites,
          [ProductId.From(Food), ProductId.From(Fuel)],
          ProductId.From(Fuel),
          vehicle.Id,
          vehicle,
          MinMargin: 0m,
          GatePrice: _ => Trade.WorldPrice,
          FuelBuyLimitPrice: Trade.WorldPrice),
        world.Hubs.Values.OrderBy(h => h.Id.Value).First().Id)
    ];
  }
}
