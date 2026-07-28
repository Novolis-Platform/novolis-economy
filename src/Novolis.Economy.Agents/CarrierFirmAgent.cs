using Novolis.Economy;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;
using Novolis.Economy.Simulation;

namespace Novolis.Economy.Agents;

/// <summary>Thresholds for a tramp / carrier firm.</summary>
public sealed record CarrierFirmAgentPolicy(
  IReadOnlyList<AgentSite> Sites,
  IReadOnlyList<ProductId> FreightProducts,
  ProductId FuelProduct,
  VehicleClassId VehicleClassId,
  VehicleClass Vehicle,
  decimal MinMargin,
  Func<ProductId, decimal> GatePrice,
  decimal FuelBuyLimitPrice,
  decimal MinBunkerFuel = 4m,
  bool AllowFuelProcurement = true);

/// <summary>Clears cross-hub sell@A + buy@B spreads via haul (heuristics + RNG ties).</summary>
public sealed class CarrierFirmAgent : IEconomicAgent
{
  private readonly CarrierFirmAgentPolicy _policy;
  private readonly Dictionary<InventoryLocationId, AgentSite> _siteByLoc;
  private readonly Dictionary<(Guid, Guid), Itinerary?> _routeCache = new();
  private readonly ulong _rngSalt;
  private TransportHubId _currentHub;
  private SpreadJob? _activeHaul;

  /// <summary>Creates the agent.</summary>
  public CarrierFirmAgent(
    FirmId firmId,
    CarrierFirmAgentPolicy policy,
    TransportHubId homeHub,
    ulong rngSalt = 0x43415252UL)
  {
    FirmId = firmId;
    _policy = policy;
    _rngSalt = rngSalt;
    _currentHub = homeHub;
    _siteByLoc = policy.Sites.ToDictionary(s => s.LocationId);
  }

  /// <inheritdoc />
  public FirmId FirmId { get; }

  /// <inheritdoc />
  public string LastDecision { get; private set; } = "standing by";

  /// <summary>Last route evaluation summary.</summary>
  public string LastEval { get; private set; } = "";

  /// <summary>Last known hub.</summary>
  public TransportHubId CurrentHub => _currentHub;

  /// <inheritdoc />
  public void Tick(AgentContext context)
  {
    var world = context.World;
    var rng = new DeterministicRandom(context.Simulation.State.Seed ^ _rngSalt ^ (ulong)FirmId.Value.GetHashCode());

    var ship = world.Shipments.FirstOrDefault(s =>
      !s.IsLegacy && s.FirmId.Equals(FirmId) && s.Status == ShipmentStatus.InTransit);
    if (ship is not null)
    {
      _currentHub = ship.CurrentHubId;
      LastDecision = $"underway @ {HubName(context, _currentHub)}";
      return;
    }

    if (world.PendingPlanShipments.Any(p => p.FirmId.Equals(FirmId)))
    {
      LastDecision = "awaiting departure";
      return;
    }

    foreach (var site in _policy.Sites.Where(s => s.HubId is not null))
    {
      foreach (var sku in _policy.FreightProducts)
      {
        var have = Qty(context, site.LocationId, sku);
        if (have < 1m)
        {
          continue;
        }

        _currentHub = site.HubId!.Value;
        if (_activeHaul is { } haul
            && haul.Product.Equals(sku)
            && haul.OriginLoc.Equals(site.LocationId)
            && have + 0.01m >= Math.Min(haul.Quantity, 1m))
        {
          if (!EnsureBunker(context, site))
          {
            LastDecision = $"bunkering @ {site.Name}";
            LastEval = haul.Summary;
            return;
          }

          var qty = Math.Min(have, haul.Quantity);
          context.Enqueue(new PlanShipment(
            FirmId, haul.OriginHub.Value, haul.DestHub.Value,
            sku, Quantity.From(qty), _policy.VehicleClassId.Value));
          LastDecision = $"haul {haul.Name} ×{qty:0}";
          LastEval = haul.Summary;
          return;
        }

        if (world.HubOrders.Any(o =>
              o.Side == HubOrderSide.Buy && !o.IsFilled
              && o.LocationId.Equals(site.LocationId)
              && o.ProductId.Equals(sku)
              && !o.FirmId.Equals(FirmId)
              && o.LimitPrice.Amount + 0.0001m >= _policy.GatePrice(sku)))
        {
          OfferLocalSale(context, site, sku, have, rng);
          _activeHaul = null;
          return;
        }

        var outbound = BestOutboundFrom(context, site, sku, have, rng);
        if (outbound is not null)
        {
          if (!EnsureBunker(context, site))
          {
            _activeHaul = outbound;
            LastDecision = $"bunkering @ {site.Name}";
            LastEval = outbound.Summary;
            return;
          }

          _activeHaul = outbound;
          var qty = Math.Min(have, outbound.Quantity);
          context.Enqueue(new PlanShipment(
            FirmId, outbound.OriginHub.Value, outbound.DestHub.Value,
            sku, Quantity.From(qty), _policy.VehicleClassId.Value));
          LastDecision = $"haul {outbound.Name} ×{qty:0}";
          LastEval = outbound.Summary;
          return;
        }

        OfferLocalSale(context, site, sku, have, rng);
        _activeHaul = null;
        return;
      }
    }

    TopUpFuelAt(context, HomeSite());
    HubOrderQuotes.CancelOpen(context, FirmId, side: HubOrderSide.Buy);

    var allJobs = BuildSpreadJobs(context);
    var candidates = allJobs
      .Where(j => j.Margin >= _policy.MinMargin)
      .OrderByDescending(j => j.Margin)
      .ThenBy(_ => rng.NextDouble())
      .ToList();
    LastEval = string.Join(" · ", candidates.Take(3).Select(c => $"{c.Name} Δ{c.Margin:0}"));
    var best = candidates.FirstOrDefault();
    if (best is null)
    {
      _activeHaul = null;
      LastDecision = "idle — no book spreads";
      if (allJobs.Count > 0)
      {
        LastEval = "below min " + string.Join(" · ", allJobs.OrderByDescending(j => j.Margin).Take(3)
          .Select(c => $"{c.Name} Δ{c.Margin:0}"));
      }

      return;
    }

    _activeHaul = best;
    var originSite = _siteByLoc[best.OriginLoc];
    if (!EnsureBunker(context, originSite))
    {
      context.Enqueue(new PostHubOrder(
        FirmId, best.OriginLoc, best.Product, HubOrderSide.Buy,
        Quantity.From(best.Quantity), Money.From(best.LiftLimit)));
      LastDecision = $"lift {best.Name} (await bunker)";
      LastEval = best.Summary;
      return;
    }

    context.Enqueue(new PostHubOrder(
      FirmId, best.OriginLoc, best.Product, HubOrderSide.Buy,
      Quantity.From(best.Quantity), Money.From(best.LiftLimit)));
    context.Enqueue(new PlanShipment(
      FirmId, best.OriginHub.Value, best.DestHub.Value,
      best.Product, Quantity.From(best.Quantity), _policy.VehicleClassId.Value));
    LastDecision = $"lift+haul {best.Name} qty {best.Quantity:0}";
    LastEval = best.Summary;
  }

  private AgentSite HomeSite() =>
    _policy.Sites.FirstOrDefault(s => s.HubId.Equals(_currentHub)) ?? _policy.Sites[0];

  private void OfferLocalSale(
    AgentContext context, AgentSite site, ProductId sku, decimal have, DeterministicRandom rng)
  {
    HubOrderQuotes.CancelOpen(context, FirmId, site.LocationId, sku);
    var bestBid = context.World.HubOrders
      .Where(o => o.Side == HubOrderSide.Buy && !o.IsFilled
                  && o.LocationId.Equals(site.LocationId)
                  && o.ProductId.Equals(sku)
                  && !o.FirmId.Equals(FirmId))
      .Select(o => o.LimitPrice.Amount)
      .DefaultIfEmpty(0m)
      .Max();
    var gate = _policy.GatePrice(sku);
    var px = bestBid > 0m
      ? bestBid * (0.98m + ((decimal)rng.NextDouble() * 0.02m))
      : gate * 1.05m;
    context.Enqueue(new PostHubOrder(
      FirmId, site.LocationId, sku, HubOrderSide.Sell,
      Quantity.From(have), Money.From(Math.Round(px, 2))));
    LastDecision = $"offer ×{have:0} @ {site.Name}";
    LastEval = bestBid > 0m ? $"clear bid {bestBid:0.##}" : "deliver into book";
  }

  private bool EnsureBunker(AgentContext context, AgentSite site)
  {
    var fuel = Qty(context, site.LocationId, _policy.FuelProduct);
    if (fuel >= _policy.MinBunkerFuel)
    {
      return true;
    }

    TopUpFuelAt(context, site);
    if (_policy.AllowFuelProcurement)
    {
      context.Enqueue(new PlaceProcurementOrder(
        FirmId, site.LocationId, _policy.FuelProduct, Quantity.From(8m),
        Money.From(_policy.FuelBuyLimitPrice * 1.2m)));
    }

    return Qty(context, site.LocationId, _policy.FuelProduct) >= _policy.MinBunkerFuel;
  }

  private void TopUpFuelAt(AgentContext context, AgentSite site)
  {
    var fuel = Qty(context, site.LocationId, _policy.FuelProduct);
    if (fuel >= 6m)
    {
      return;
    }

    HubOrderQuotes.CancelOpen(context, FirmId, site.LocationId, _policy.FuelProduct);
    context.Enqueue(new PostHubOrder(
      FirmId, site.LocationId, _policy.FuelProduct, HubOrderSide.Buy, Quantity.From(12m),
      Money.From(_policy.FuelBuyLimitPrice)));
  }

  private SpreadJob? BestOutboundFrom(
    AgentContext context, AgentSite origin, ProductId sku, decimal have, DeterministicRandom rng)
  {
    if (origin.HubId is null)
    {
      return null;
    }

    var world = context.World;
    var wage = world.Policy.WageRatePerHour;
    var fuelCost = world.TransportFuelUnitCost;
    SpreadJob? best = null;
    foreach (var buy in world.HubOrders
               .Where(b => b.Side == HubOrderSide.Buy && !b.IsFilled
                           && b.ProductId.Equals(sku)
                           && !b.LocationId.Equals(origin.LocationId)
                           && !b.FirmId.Equals(FirmId))
               .OrderByDescending(b => b.LimitPrice.Amount)
               .ThenBy(_ => rng.NextDouble())
               .Take(25))
    {
      if (!_siteByLoc.TryGetValue(buy.LocationId, out var dest) || dest.HubId is null)
      {
        continue;
      }

      var qty = Math.Min(have, Math.Min(buy.Remaining.Value, _policy.Vehicle.CargoCapacity.Value));
      if (qty < 1m || !TryGetRoute(origin.HubId.Value, dest.HubId.Value, world, out var itinerary))
      {
        continue;
      }

      var est = HaulCostEstimator.Estimate(itinerary, world.Corridors, _policy.Vehicle, wage, fuelCost);
      var cog = _policy.GatePrice(sku);
      var margin = qty * buy.LimitPrice.Amount - qty * cog - est.TotalVariableCost.Amount;
      // Holding inventory: always consider destinations — even negative Δ — so the hull is not trapped.
      var job = new SpreadJob(
        $"{Short(sku)} {ShortName(origin.Name)}→{ShortName(dest.Name)}",
        origin.HubId.Value, dest.HubId.Value, origin.LocationId, dest.LocationId,
        sku, qty, cog, buy.LimitPrice.Amount, margin,
        $"Δ{margin:0.#} haul {est.TotalVariableCost.Amount:0}");
      if (best is null || job.Margin > best.Margin)
      {
        best = job;
      }
    }

    return best;
  }

  private List<SpreadJob> BuildSpreadJobs(AgentContext context)
  {
    var world = context.World;
    var wage = world.Policy.WageRatePerHour;
    var fuelCost = world.TransportFuelUnitCost;
    var freight = _policy.FreightProducts;
    var sellsByProduct = new Dictionary<ProductId, List<HubOrder>>();
    var buysByProduct = new Dictionary<ProductId, List<HubOrder>>();
    foreach (var o in world.HubOrders)
    {
      if (o.IsFilled || o.FirmId.Equals(FirmId) || !freight.Any(p => p.Equals(o.ProductId)))
      {
        continue;
      }

      if (o.Side == HubOrderSide.Sell)
      {
        if (!sellsByProduct.TryGetValue(o.ProductId, out var sells))
        {
          sells = new List<HubOrder>(16);
          sellsByProduct[o.ProductId] = sells;
        }

        sells.Add(o);
      }
      else
      {
        if (!buysByProduct.TryGetValue(o.ProductId, out var buys))
        {
          buys = new List<HubOrder>(16);
          buysByProduct[o.ProductId] = buys;
        }

        buys.Add(o);
      }
    }

    var jobs = new List<SpreadJob>();
    foreach (var (product, rawSells) in sellsByProduct)
    {
      if (!buysByProduct.TryGetValue(product, out var rawBuys))
      {
        continue;
      }

      // Prefer deep / cheap sells and rich buys — avoid fuel-noise FIFO truncation.
      var sells = rawSells
        .OrderBy(s => s.LimitPrice.Amount)
        .ThenByDescending(s => s.Remaining.Value)
        .Take(24)
        .ToList();
      var buys = rawBuys
        .OrderByDescending(b => b.LimitPrice.Amount)
        .ThenByDescending(b => b.Remaining.Value)
        .Take(24)
        .ToList();

      foreach (var sell in sells)
      {
        if (!_siteByLoc.TryGetValue(sell.LocationId, out var origin) || origin.HubId is null)
        {
          continue;
        }

        var matched = 0;
        foreach (var buy in buys)
        {
          if (matched >= 20
              || buy.LocationId.Equals(sell.LocationId)
              || buy.LimitPrice.Amount < sell.LimitPrice.Amount
              || !_siteByLoc.TryGetValue(buy.LocationId, out var dest)
              || dest.HubId is null)
          {
            continue;
          }

          var qty = Math.Min(Math.Min(sell.Remaining.Value, buy.Remaining.Value), _policy.Vehicle.CargoCapacity.Value);
          if (qty < 2m || !TryGetRoute(origin.HubId.Value, dest.HubId.Value, world, out var itinerary))
          {
            continue;
          }

          matched++;
          var est = HaulCostEstimator.Estimate(itinerary, world.Corridors, _policy.Vehicle, wage, fuelCost);
          var lift = Math.Min(buy.LimitPrice.Amount, sell.LimitPrice.Amount * 1.12m);
          var margin = qty * buy.LimitPrice.Amount - qty * lift - est.TotalVariableCost.Amount;
          jobs.Add(new SpreadJob(
            $"{Short(sell.ProductId)} {ShortName(origin.Name)}→{ShortName(dest.Name)}",
            origin.HubId.Value, dest.HubId.Value, sell.LocationId, dest.LocationId,
            sell.ProductId, qty, lift, buy.LimitPrice.Amount, margin,
            $"Δ{margin:0.#} haul {est.TotalVariableCost.Amount:0}"));
        }
      }
    }

    return jobs;
  }

  private bool TryGetRoute(
    TransportHubId origin, TransportHubId dest, EconomyWorld world, out Itinerary itinerary)
  {
    var key = (origin.Value, dest.Value);
    if (_routeCache.TryGetValue(key, out var cached))
    {
      if (cached is null)
      {
        itinerary = default!;
        return false;
      }

      itinerary = cached;
      return true;
    }

    if (!ItineraryPlanner.TryPlan(
          origin, dest, _policy.Vehicle.CargoCapacity, _policy.Vehicle, world.Corridors, out itinerary))
    {
      _routeCache[key] = null;
      return false;
    }

    _routeCache[key] = itinerary;
    return true;
  }

  private decimal Qty(AgentContext context, InventoryLocationId loc, ProductId p) =>
    context.World.Inventory.GetQuantity(new InventoryKey(FirmId, loc, p)).Value;

  private static string HubName(AgentContext context, TransportHubId id) =>
    context.World.Hubs.TryGetValue(id, out var h) ? h.Name : "?";

  private string Short(ProductId p) => p.Value.ToString("N")[..4];

  private static string ShortName(string name) =>
    name.Length <= 14 ? name : name[..12] + "…";

  private sealed record SpreadJob(
    string Name,
    TransportHubId OriginHub,
    TransportHubId DestHub,
    InventoryLocationId OriginLoc,
    InventoryLocationId DestLoc,
    ProductId Product,
    decimal Quantity,
    decimal LiftLimit,
    decimal DestBid,
    decimal Margin,
    string Summary);
}
