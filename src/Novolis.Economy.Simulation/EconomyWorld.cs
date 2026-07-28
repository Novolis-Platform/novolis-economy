using System.Collections.Immutable;
using Novolis.Economy;
using Novolis.Economy.Accounting;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation;

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
    FacilityLayout layout)
  {
    Id = id;
    FirmId = firmId;
    StorageLocation = storageLocation;
    RetailLocation = retailLocation;
    Layout = layout;
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

  /// <summary>Ledgers by firm.</summary>
  public Dictionary<FirmId, FirmLedger> Ledgers { get; } = new();

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

  /// <summary>Restock routes: facility storage → retail (optional auto-restock).</summary>
  public Dictionary<FacilityId, FreightRouteId> RestockRoutes { get; } = new();

  /// <summary>Active shipments.</summary>
  public List<ActiveShipment> Shipments { get; } = [];

  /// <summary>Pending procurement orders.</summary>
  public List<PlaceProcurementOrder> PendingProcurement { get; } = [];

  /// <summary>Pending shipment commands.</summary>
  public List<IssueShipment> PendingShipments { get; } = [];

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

  /// <summary>Research budget remaining (cash reserved conceptually).</summary>
  public Dictionary<FirmId, Money> ResearchBudget { get; } = new();

  /// <summary>Productivity factor (>= 0.01).</summary>
  public Dictionary<FirmId, decimal> Productivity { get; } = new();

  /// <summary>Market book.</summary>
  public ObservedMarketBook MarketBook { get; } = new();

  /// <summary>Default geographic area for market estimates.</summary>
  public GeographicAreaId DefaultArea { get; set; } = GeographicAreaId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

  /// <summary>Ensures a firm ledger and defaults exist.</summary>
  public FirmLedger EnsureFirm(FirmId firmId, string name)
  {
    Firms[firmId] = name;
    if (!Ledgers.TryGetValue(firmId, out var ledger))
    {
      ledger = new FirmLedger(firmId);
      Ledgers[firmId] = ledger;
    }

    Productivity.TryAdd(firmId, 1m);
    AvailableLaborHours.TryAdd(firmId, 8m);
    AccruedWages.TryAdd(firmId, Money.Zero);
    return ledger;
  }

  /// <summary>Retail facility map for demand.</summary>
  public Dictionary<FacilityId, (FirmId Firm, InventoryLocationId RetailLocation)> RetailFacilityMap()
  {
    var map = new Dictionary<FacilityId, (FirmId, InventoryLocationId)>();
    foreach (var facility in Facilities.Values)
    {
      if (facility.RetailLocation is { } retail)
      {
        map[facility.Id] = (facility.FirmId, retail);
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

    return hash;
  }
}
