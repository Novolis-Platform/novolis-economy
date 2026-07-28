using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Core;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

/// <summary>How cohort budgets behave at accounting period close.</summary>
public enum CohortBudgetResetMode
{
  /// <summary>Remint each cohort to its disposable income (legacy open mint).</summary>
  MintFromDisposableIncome = 0,

  /// <summary>Leave <c>BudgetRemaining</c> unchanged (closed-loop credit stock).</summary>
  CarryForward = 1,
}

/// <summary>Simulation policy knobs.</summary>
public sealed class EconomyPolicy
{
  /// <summary>Wage rate per labor hour.</summary>
  public Money WageRatePerHour { get; init; } = Money.From(10m);

  /// <summary>Labor hours required per output unit (default).</summary>
  public decimal LaborHoursPerOutputUnit { get; init; } = 0.1m;

  /// <summary>Accounting period length in hours.</summary>
  public int PeriodHours { get; init; } = SimulationHour.HoursPerDay;

  /// <summary>When true, shelf-life spoilage runs each tick.</summary>
  public bool EnableSpoilage { get; init; }

  /// <summary>Research spend converts to productivity at this rate (per currency unit).</summary>
  public decimal ResearchProductivityPerCurrency { get; init; } = 0.0001m;

  /// <summary>
  /// When true, paid wages increase cohort <c>BudgetRemaining</c> (population-weighted)
  /// so household spending power replaces destroyed firm cash.
  /// Default false preserves legacy wage cash destruction.
  /// </summary>
  public bool HouseholdCreditFromWages { get; init; }

  /// <summary>
  /// Period-close budget policy. Default remints disposable income; use
  /// <see cref="CohortBudgetResetMode.CarryForward"/> for closed-loop money stock.
  /// </summary>
  public CohortBudgetResetMode CohortBudgetResetMode { get; init; } =
    CohortBudgetResetMode.MintFromDisposableIncome;

  /// <summary>
  /// When set, corridor tolls debit the shipper and credit this firm's cash/revenue
  /// (liquid cash conserved). Null keeps legacy “toll burns cash” behavior.
  /// </summary>
  public FirmId? TollBeneficiaryFirmId { get; init; }

  /// <summary>
  /// Retail price elasticity for <see cref="Population.DemandEngine"/>.
  /// 0 = legacy (ignore price vs reference); typical soft values 0.5–1.5.
  /// </summary>
  public decimal PriceElasticity { get; init; }

  /// <summary>Comfort floor per household; invest/lend require budget above floor × count.</summary>
  public Money HouseholdComfortThresholdPerHousehold { get; init; } = Money.From(50m);

  /// <summary>
  /// When true (default if any region exists), firm labor supply comes from region household pools.
  /// </summary>
  public bool UseRegionLaborPools { get; init; } = true;
}

/// <summary>Facility binding to firm and inventory locations.</summary>
public sealed class FacilityBinding
{
  /// <summary>Creates a facility binding.</summary>
  public FacilityBinding(
    FacilityId id,
    FirmId firmId,
    InventoryLocationId storageLocation,
    InventoryLocationId? retailLocation,
    FacilityLayout layout,
    GeographicAreaId? area = null)
  {
    Id = id;
    FirmId = firmId;
    StorageLocation = storageLocation;
    RetailLocation = retailLocation;
    Layout = layout;
    Area = area;
  }

  /// <summary>Facility id.</summary>
  public FacilityId Id { get; }

  /// <summary>Owning firm.</summary>
  public FirmId FirmId { get; }

  /// <summary>Primary storage location.</summary>
  public InventoryLocationId StorageLocation { get; }

  /// <summary>Optional retail shelf location.</summary>
  public InventoryLocationId? RetailLocation { get; }

  /// <summary>Layout graph.</summary>
  public FacilityLayout Layout { get; }

  /// <summary>
  /// Optional geographic area for local demand. Null = visible to all cohorts
  /// (legacy / global retail).
  /// </summary>
  public GeographicAreaId? Area { get; }

  /// <summary>Manufacturing capacity summed from manufacturing units.</summary>
  public Quantity ManufacturingCapacity =>
    Quantity.From(
      Layout.Units.Values
        .Where(u => u.Kind is OperatingUnitKind.Manufacturing or OperatingUnitKind.Assembly)
        .Sum(u => u.Capacity.Value));
}

/// <summary>Mutable economic world hosted by <see cref="SimulationState"/>.</summary>
public sealed class EconomyWorld
{
  /// <summary>Creates an empty world.</summary>
  public EconomyWorld(EconomyPolicy? policy = null)
  {
    Policy = policy ?? new EconomyPolicy();
  }

  /// <summary>Policy.</summary>
  public EconomyPolicy Policy { get; }

  /// <summary>Product catalog.</summary>
  public Dictionary<ProductId, ProductDefinition> Products { get; } = new();

  /// <summary>Firm display names.</summary>
  public Dictionary<FirmId, string> Firms { get; } = new();

  /// <summary>Legal-entity metadata keyed by firm id (Firm / Civic / Household).</summary>
  public Dictionary<FirmId, LegalEntity> Entities { get; } = new();

  /// <summary>Habitat/regions keyed by area.</summary>
  public Dictionary<GeographicAreaId, EconomicRegion> Regions { get; } = new();

  /// <summary>Ledgers by firm.</summary>
  public Dictionary<FirmId, FirmLedger> Ledgers { get; } = new();

  /// <summary>Ownership claims (issuer → owners).</summary>
  public List<OwnershipClaim> OwnershipClaims { get; } = [];

  /// <summary>Facilities.</summary>
  public Dictionary<FacilityId, FacilityBinding> Facilities { get; } = new();

  /// <summary>Inventory lots.</summary>
  public InventoryStore Inventory { get; } = new();

  /// <summary>Posted retail prices.</summary>
  public Dictionary<(FirmId Firm, FacilityId Facility, ProductId Product), Money> RetailPrices { get; } = new();

  /// <summary>Production plans (units per hour).</summary>
  public Dictionary<(FirmId Firm, FacilityId Facility, ProductId Product), Quantity> ProductionPlans { get; } = new();

  /// <summary>Freight routes.</summary>
  public Dictionary<FreightRouteId, FreightRoute> Routes { get; } = new();

  /// <summary>Transport hubs.</summary>
  public Dictionary<TransportHubId, TransportHub> Hubs { get; } = new();

  /// <summary>Directed corridors between hubs.</summary>
  public Dictionary<TransportCorridorId, TransportCorridor> Corridors { get; } = new();

  /// <summary>Vehicle classes.</summary>
  public Dictionary<VehicleClassId, VehicleClass> VehicleClasses { get; } = new();

  /// <summary>Optional default fuel product for multi-leg transport.</summary>
  public ProductId? TransportFuelProductId { get; set; }

  /// <summary>Unit cost used when writing off burned fuel (defaults to 1).</summary>
  public Money TransportFuelUnitCost { get; set; } = Money.From(1m);

  /// <summary>Restock routes: facility storage → retail (optional auto-restock).</summary>
  public Dictionary<FacilityId, FreightRouteId> RestockRoutes { get; } = new();

  /// <summary>Active shipments.</summary>
  public List<ActiveShipment> Shipments { get; } = [];

  /// <summary>Pending procurement orders.</summary>
  public List<PlaceProcurementOrder> PendingProcurement { get; } = [];

  /// <summary>Pending export orders (exogenous demand).</summary>
  public List<PlaceExportOrder> PendingExports { get; } = [];

  /// <summary>Pending shipment commands.</summary>
  public List<IssueShipment> PendingShipments { get; } = [];

  /// <summary>Pending multi-leg plan commands.</summary>
  public List<PlanShipment> PendingPlanShipments { get; } = [];

  /// <summary>Cumulative transport aggregates (scenario reporting).</summary>
  public TransportAggregates TransportStats { get; } = new();

  /// <summary>Consumer cohorts.</summary>
  public List<CohortState> Cohorts { get; } = [];

  /// <summary>Available labor hours per firm per tick.</summary>
  public Dictionary<FirmId, decimal> AvailableLaborHours { get; } = new();

  /// <summary>Labor allocated this tick to manufacturing.</summary>
  public Dictionary<FirmId, decimal> AllocatedLaborHours { get; } = new();

  /// <summary>Accrued unpaid wages.</summary>
  public Dictionary<FirmId, Money> AccruedWages { get; } = new();

  /// <summary>Open invoices.</summary>
  public List<Invoice> Invoices { get; } = [];

  /// <summary>Open hub spot orders.</summary>
  public List<Novolis.Economy.Markets.HubOrder> HubOrders { get; } = [];

  /// <summary>Inter-firm term loans.</summary>
  public List<Novolis.Economy.Finance.Loan> Loans { get; } = [];

  /// <summary>Research budget remaining (cash reserved conceptually).</summary>
  public Dictionary<FirmId, Money> ResearchBudget { get; } = new();

  /// <summary>Productivity factor (>= 0.01).</summary>
  public Dictionary<FirmId, decimal> Productivity { get; } = new();

  /// <summary>Market book.</summary>
  public ObservedMarketBook MarketBook { get; } = new();

  /// <summary>
  /// Core bounded-minimum aggregate (economic authority). Replaced immutably on period Advance
  /// and when Logistics deliveries credit holdings.
  /// </summary>
  public EconomyState CoreState { get; set; } = EconomyState.Empty;

  /// <summary>Ops hub → Core region map (carriage destination).</summary>
  public Dictionary<TransportHubId, RegionId> HubRegions { get; } = new();

  /// <summary>Default geographic area for market estimates.</summary>
  public GeographicAreaId DefaultArea { get; set; } = GeographicAreaId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

  /// <summary>Ensures a firm ledger, legal entity (Firm), and defaults exist.</summary>
  public FirmLedger EnsureFirm(FirmId firmId, string name)
  {
    Firms[firmId] = name;
    if (!Ledgers.TryGetValue(firmId, out var ledger))
    {
      ledger = new FirmLedger(firmId);
      Ledgers[firmId] = ledger;
    }

    Entities.TryAdd(firmId, new LegalEntity(firmId, LegalEntityKind.Firm));
    Productivity.TryAdd(firmId, 1m);
    AvailableLaborHours.TryAdd(firmId, 8m);
    AccruedWages.TryAdd(firmId, Money.Zero);
    return ledger;
  }

  /// <summary>Ensures civic legal-entity metadata (ledger via <see cref="EnsureFirm"/>).</summary>
  public LegalEntity EnsureCivic(FirmId firmId, string name, string? registryId = null)
  {
    EnsureFirm(firmId, name);
    var entity = new LegalEntity(firmId, LegalEntityKind.Civic, registryId);
    Entities[firmId] = entity;
    return entity;
  }

  /// <summary>Ensures a household party (ledger present; spendable cash is cohort budget).</summary>
  public LegalEntity EnsureHousehold(FirmId firmId, string name)
  {
    EnsureFirm(firmId, name);
    var entity = new LegalEntity(firmId, LegalEntityKind.Household);
    Entities[firmId] = entity;
    AvailableLaborHours[firmId] = 0m;
    return entity;
  }

  /// <summary>Whether the id is a household legal entity.</summary>
  public bool IsHousehold(FirmId firmId) =>
    Entities.TryGetValue(firmId, out var e) && e.Kind == LegalEntityKind.Household;

  /// <summary>Whether the firm may issue ownership shares.</summary>
  public bool CanIssueShares(FirmId firmId) =>
    Entities.TryGetValue(firmId, out var e) && e.CanIssueShares;

  /// <summary>Whether the firm is credit-frozen.</summary>
  public bool IsCreditFrozen(FirmId firmId) =>
    Entities.TryGetValue(firmId, out var e) && e.CreditFrozen;

  /// <summary>Households currently living in an area.</summary>
  public int UsedLivingHouseholds(GeographicAreaId area) =>
    Cohorts
      .Where(c => c.Definition.Area.Equals(area))
      .Sum(c => Population.HouseholdMath.Count(c.Definition.Population));

  /// <summary>Mfg/assembly facilities in an area.</summary>
  public int UsedProductionSlots(GeographicAreaId area) =>
    Facilities.Values.Count(f =>
      f.Area is { } a
      && a.Equals(area)
      && EconomicRegion.ConsumesProductionSlot(f.Layout));

  /// <summary>Whether region labor pools should drive firm availability.</summary>
  public bool RegionLaborPoolsActive =>
    Policy.UseRegionLaborPools && Regions.Count > 0;

  /// <summary>Cohort linked to a household firm id, if any.</summary>
  public CohortState? FindCohortByHousehold(FirmId householdFirmId) =>
    Cohorts.FirstOrDefault(c =>
      c.Definition.HouseholdFirmId is { } hid && hid.Equals(householdFirmId));

  /// <summary>Comfort floor for a cohort under current policy.</summary>
  public Money ComfortFloor(CohortState cohort) =>
    Population.HouseholdMath.ComfortFloor(
      cohort.Definition.Population,
      Policy.HouseholdComfortThresholdPerHousehold);

  /// <summary>True when budget is strictly above the comfort floor.</summary>
  public bool IsAboveComfort(CohortState cohort) =>
    cohort.BudgetRemaining.Amount > ComfortFloor(cohort).Amount;

  /// <summary>Debits household cohort budget; returns false if insufficient.</summary>
  public bool TryDebitHouseholdBudget(FirmId householdFirmId, Money amount)
  {
    if (amount.Amount <= 0m)
    {
      return true;
    }

    var cohort = FindCohortByHousehold(householdFirmId);
    if (cohort is null || cohort.BudgetRemaining.Amount + 0.0000001m < amount.Amount)
    {
      return false;
    }

    cohort.BudgetRemaining = Money.From(cohort.BudgetRemaining.Amount - amount.Amount);
    return true;
  }

  /// <summary>Credits household cohort budget.</summary>
  public void CreditHouseholdBudget(FirmId householdFirmId, Money amount)
  {
    if (amount.Amount <= 0m)
    {
      return;
    }

    var cohort = FindCohortByHousehold(householdFirmId);
    if (cohort is null)
    {
      return;
    }

    cohort.BudgetRemaining = Money.From(cohort.BudgetRemaining.Amount + amount.Amount);
  }

  /// <summary>Retail facility map for demand (includes optional area for local clearing).</summary>
  public Dictionary<FacilityId, (FirmId Firm, InventoryLocationId RetailLocation, GeographicAreaId? Area)> RetailFacilityMap()
  {
    var map = new Dictionary<FacilityId, (FirmId, InventoryLocationId, GeographicAreaId?)>();
    foreach (var facility in Facilities.Values)
    {
      if (facility.RetailLocation is { } retail)
      {
        map[facility.Id] = (facility.FirmId, retail, facility.Area);
      }
    }

    return map;
  }

  /// <summary>World fingerprint for determinism hashing.</summary>
  public ulong Fingerprint()
  {
    const ulong offset = 14695981039346656037UL;
    const ulong prime = 1099511628211UL;
    var hash = offset;
    hash = (hash ^ Inventory.Fingerprint()) * prime;
    hash = (hash ^ MarketBook.Fingerprint()) * prime;
    foreach (var ledger in Ledgers.Values.OrderBy(l => l.FirmId.Value))
    {
      hash = (hash ^ ledger.Fingerprint()) * prime;
    }

    hash = (hash ^ (ulong)Shipments.Count(s => s.Status == ShipmentStatus.InTransit)) * prime;
    hash = (hash ^ (ulong)RetailPrices.Count) * prime;
    foreach (var (key, price) in RetailPrices.OrderBy(kv => kv.Key.Firm.Value).ThenBy(kv => kv.Key.Product.Value))
    {
      hash = (hash ^ (ulong)key.Product.Value.GetHashCode()) * prime;
      foreach (var b in decimal.GetBits(price.Amount))
      {
        hash = (hash ^ (ulong)(uint)b) * prime;
      }
    }

    foreach (var cohort in Cohorts.OrderBy(c => c.Definition.Id.Value))
    {
      foreach (var b in decimal.GetBits(cohort.BudgetRemaining.Amount))
      {
        hash = (hash ^ (ulong)(uint)b) * prime;
      }
    }

    foreach (var entity in Entities.Values.OrderBy(e => e.Id.Value))
    {
      hash = (hash ^ (ulong)(uint)entity.Kind) * prime;
      hash = (hash ^ (entity.CreditFrozen ? 1UL : 0UL)) * prime;
      if (entity.RegistryId is { } reg)
      {
        hash = (hash ^ (ulong)(uint)reg.GetHashCode()) * prime;
      }
    }

    foreach (var claim in OwnershipClaims
               .OrderBy(c => c.IssuerFirmId.Value)
               .ThenBy(c => c.OwnerFirmId.Value))
    {
      hash = (hash ^ (ulong)claim.IssuerFirmId.Value.GetHashCode()) * prime;
      hash = (hash ^ (ulong)claim.OwnerFirmId.Value.GetHashCode()) * prime;
      foreach (var b in decimal.GetBits(claim.Fraction))
      {
        hash = (hash ^ (ulong)(uint)b) * prime;
      }
    }

    return hash;
  }
}

/// <summary>Running transport economics counters for scenarios.</summary>
public sealed class TransportAggregates
{
  /// <summary>Cargo quantity delivered via multi-leg shipments.</summary>
  public Quantity CargoDelivered { get; set; }

  /// <summary>Fuel units burned.</summary>
  public Quantity FuelBurned { get; set; }

  /// <summary>Ledger value of burned fuel.</summary>
  public Money FuelBurnValue { get; set; }

  /// <summary>Fuel units bunkered from hubs.</summary>
  public Quantity FuelBunkered { get; set; }

  /// <summary>Tolls paid.</summary>
  public Money TollsPaid { get; set; }

  /// <summary>Crew labor hours while underway.</summary>
  public decimal CrewLaborHours { get; set; }

  /// <summary>Failed plan attempts.</summary>
  public int FailedPlans { get; set; }

  /// <summary>Sum of hours from depart to deliver for completed multi-leg shipments.</summary>
  public long TransitHoursSum { get; set; }

  /// <summary>Count of completed multi-leg deliveries (for average transit).</summary>
  public int TransitSampleCount { get; set; }
}
