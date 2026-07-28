using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>One manufactured SKU plan + sell rule.</summary>
public sealed record ManufacturedSkuPolicy(
  ProductId ProductId,
  decimal BaseRate,
  decimal StockTarget,
  decimal MinInputOnHand,
  ProductId? RequiredInput,
  decimal SellAboveStock,
  decimal SellKeepFloor,
  decimal SellMaxQty,
  decimal GatePrice);

/// <summary>Thresholds for a multi-product plant.</summary>
public sealed record ManufacturingFirmAgentPolicy(
  IReadOnlyList<AgentSite> Sites,
  ProductId PrimaryInput,
  decimal PrimaryInputFloor,
  decimal PrimaryInputLimitPrice,
  IReadOnlyList<ManufacturedSkuPolicy> Outputs,
  decimal PriceJitter = 0.04m);

/// <summary>Buys primary input, runs throttled plans, sells outputs on the hub book.</summary>
public sealed class ManufacturingFirmAgent : IEconomicAgent
{
  private readonly ManufacturingFirmAgentPolicy _policy;
  private readonly ulong _rngSalt;

  /// <summary>Creates the agent.</summary>
  public ManufacturingFirmAgent(FirmId firmId, ManufacturingFirmAgentPolicy policy, ulong rngSalt = 0x4D4647UL)
  {
    FirmId = firmId;
    _policy = policy;
    _rngSalt = rngSalt;
  }

  /// <inheritdoc />
  public FirmId FirmId { get; }

  /// <inheritdoc />
  public string LastDecision { get; private set; } = "manufacturing idle";

  /// <inheritdoc />
  public void Tick(AgentContext context)
  {
    HubOrderQuotes.CancelOpen(context, FirmId);
    var rng = new DeterministicRandom(context.Simulation.State.Seed ^ _rngSalt ^ (ulong)FirmId.Value.GetHashCode());
    foreach (var site in _policy.Sites.Where(s => s.FacilityId is not null))
    {
      var loc = site.LocationId;
      var facility = site.FacilityId!.Value;
      var primary = Qty(context, loc, _policy.PrimaryInput);
      if (primary < _policy.PrimaryInputFloor)
      {
        var need = Math.Max(8m, _policy.PrimaryInputFloor - primary + 10m);
        var px = _policy.PrimaryInputLimitPrice * (1m + ((decimal)rng.NextDouble() * _policy.PriceJitter - _policy.PriceJitter * 0.5m));
        context.Enqueue(new PostHubOrder(
          FirmId, loc, _policy.PrimaryInput, HubOrderSide.Buy,
          Quantity.From(need), Money.From(Math.Round(px, 2))));
      }

      foreach (var sku in _policy.Outputs)
      {
        var onHand = Qty(context, loc, sku.ProductId);
        var inputOk = sku.RequiredInput is null
          || Qty(context, loc, sku.RequiredInput.Value) >= sku.MinInputOnHand;
        var rate = ProductionThrottle.Rate(inputOk ? sku.BaseRate : 0m, onHand, sku.StockTarget);
        context.Enqueue(new SetProductionPlan(FirmId, facility, sku.ProductId, Quantity.From(rate)));

        if (onHand > sku.SellAboveStock)
        {
          var q = Math.Min(sku.SellMaxQty, onHand - sku.SellKeepFloor);
          var px = sku.GatePrice * (1m + ((decimal)rng.NextDouble() * _policy.PriceJitter - _policy.PriceJitter * 0.5m));
          context.Enqueue(new PostHubOrder(
            FirmId, loc, sku.ProductId, HubOrderSide.Sell,
            Quantity.From(q), Money.From(Math.Round(px, 2))));
        }
      }

      LastDecision = $"plant @ {site.Name}";
    }
  }

  private decimal Qty(AgentContext context, InventoryLocationId loc, ProductId p) =>
    context.World.Inventory.GetQuantity(new InventoryKey(FirmId, loc, p)).Value;
}
