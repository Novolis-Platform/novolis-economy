using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>Retail SKU shelf + replenishment.</summary>
public sealed record RetailSkuPolicy(
  ProductId ProductId,
  decimal BaseRetailPrice,
  decimal StockTarget,
  decimal DeliveredLimitPrice,
  bool PostRetailPrice);

/// <summary>Bunker / energy stock policy at a site.</summary>
public sealed record BunkerSkuPolicy(
  ProductId ProductId,
  decimal MinStock,
  decimal BuyLimitPrice,
  decimal SellPrice,
  bool AllowProcurement);

/// <summary>Thresholds for retail + bunker sites.</summary>
public sealed record RetailFirmAgentPolicy(
  IReadOnlyList<AgentSite> RetailSites,
  IReadOnlyList<AgentSite> BunkerSites,
  IReadOnlyList<RetailSkuPolicy> RetailSkus,
  BunkerSkuPolicy? Bunker,
  decimal PriceJitter = 0.04m);

/// <summary>Posts retail prices, buys stock, manages bunker inventory.</summary>
public sealed class RetailFirmAgent : IEconomicAgent
{
  private readonly RetailFirmAgentPolicy _policy;
  private readonly ulong _rngSalt;

  /// <summary>Creates the agent.</summary>
  public RetailFirmAgent(FirmId firmId, RetailFirmAgentPolicy policy, ulong rngSalt = 0x524554UL)
  {
    FirmId = firmId;
    _policy = policy;
    _rngSalt = rngSalt;
  }

  /// <inheritdoc />
  public FirmId FirmId { get; }

  /// <inheritdoc />
  public string LastDecision { get; private set; } = "retail idle";

  /// <inheritdoc />
  public void Tick(AgentContext context)
  {
    HubOrderQuotes.CancelOpen(context, FirmId);
    var rng = new DeterministicRandom(context.Simulation.State.Seed ^ _rngSalt ^ (ulong)FirmId.Value.GetHashCode());
    var world = context.World;

    foreach (var site in _policy.RetailSites.Where(s => s.FacilityId is not null))
    {
      var loc = site.LocationId;
      var facility = site.FacilityId!.Value;
      foreach (var sku in _policy.RetailSkus)
      {
        var stock = Qty(context, loc, sku.ProductId);
        if (sku.PostRetailPrice)
        {
          var price = InventoryPressurePricing.Adjust(
            Money.From(sku.BaseRetailPrice), stock, sku.StockTarget);
          context.Enqueue(new SetRetailPrice(FirmId, facility, sku.ProductId, price));
        }

        if (stock < sku.StockTarget)
        {
          var need = sku.StockTarget - stock + 5m;
          var px = sku.DeliveredLimitPrice * (1m + ((decimal)rng.NextDouble() * _policy.PriceJitter - _policy.PriceJitter * 0.5m));
          context.Enqueue(new PostHubOrder(
            FirmId, loc, sku.ProductId, HubOrderSide.Buy,
            Quantity.From(need), Money.From(Math.Round(px, 2))));
          LastDecision = $"bid ×{need:0} @ {site.Name}";
        }
      }
    }

    if (_policy.Bunker is { } bunker)
    {
      foreach (var site in _policy.BunkerSites)
      {
        var loc = site.LocationId;
        var fuel = Qty(context, loc, bunker.ProductId);
        if (fuel < bunker.MinStock)
        {
          var need = bunker.MinStock - fuel + 4m;
          context.Enqueue(new PostHubOrder(
            FirmId, loc, bunker.ProductId, HubOrderSide.Buy,
            Quantity.From(need), Money.From(bunker.BuyLimitPrice)));
          if (bunker.AllowProcurement && world.Ledgers[FirmId].Cash.Amount > bunker.BuyLimitPrice * 10m)
          {
            context.Enqueue(new PlaceProcurementOrder(
              FirmId, loc, bunker.ProductId, Quantity.From(Math.Max(8m, need)),
              Money.From(bunker.BuyLimitPrice)));
            LastDecision = $"import bunker @ {site.Name}";
          }
        }
        else if (fuel > bunker.MinStock + 6m)
        {
          var q = Math.Min(12m, fuel - bunker.MinStock);
          context.Enqueue(new PostHubOrder(
            FirmId, loc, bunker.ProductId, HubOrderSide.Sell,
            Quantity.From(q), Money.From(bunker.SellPrice)));
        }
      }
    }
  }

  private decimal Qty(AgentContext context, InventoryLocationId loc, ProductId p) =>
    context.World.Inventory.GetQuantity(new InventoryKey(FirmId, loc, p)).Value;
}
