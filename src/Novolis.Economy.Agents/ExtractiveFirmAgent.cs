using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>Thresholds for an extractive (primary-output) firm.</summary>
public sealed record ExtractiveFirmAgentPolicy(
  IReadOnlyList<AgentSite> Sites,
  ProductId OutputProduct,
  ProductId InputProduct,
  decimal BaseOutputRate,
  decimal OutputCap,
  decimal InputPerOutput,
  decimal InputFloor,
  decimal SellAboveStock,
  decimal SellKeepFloor,
  decimal SellMaxQty,
  decimal OutputGatePrice,
  decimal InputLimitPrice,
  decimal PriceJitter = 0.04m);

/// <summary>Produces one output, sells surplus, buys input when low.</summary>
public sealed class ExtractiveFirmAgent : IEconomicAgent
{
  private readonly ExtractiveFirmAgentPolicy _policy;
  private readonly ulong _rngSalt;

  /// <summary>Creates the agent.</summary>
  public ExtractiveFirmAgent(FirmId firmId, ExtractiveFirmAgentPolicy policy, ulong rngSalt = 0x45585452UL)
  {
    FirmId = firmId;
    _policy = policy;
    _rngSalt = rngSalt;
  }

  /// <inheritdoc />
  public FirmId FirmId { get; }

  /// <inheritdoc />
  public string LastDecision { get; private set; } = "extractive idle";

  /// <inheritdoc />
  public void Tick(AgentContext context)
  {
    HubOrderQuotes.CancelOpen(context, FirmId);
    var rng = new DeterministicRandom(context.Simulation.State.Seed ^ _rngSalt ^ (ulong)FirmId.Value.GetHashCode());
    foreach (var site in _policy.Sites.Where(s => s.FacilityId is not null))
    {
      var loc = site.LocationId;
      var facility = site.FacilityId!.Value;
      var output = Qty(context, loc, _policy.OutputProduct);
      var input = Qty(context, loc, _policy.InputProduct);
      var rate = input < _policy.InputPerOutput
        ? 0m
        : ProductionThrottle.Rate(_policy.BaseOutputRate, output, _policy.OutputCap);
      context.Enqueue(new SetProductionPlan(FirmId, facility, _policy.OutputProduct, Quantity.From(rate)));

      if (output > _policy.SellAboveStock)
      {
        var sellQty = Math.Min(_policy.SellMaxQty, output - _policy.SellKeepFloor);
        var px = _policy.OutputGatePrice * (1m + ((decimal)rng.NextDouble() * _policy.PriceJitter * 2m - _policy.PriceJitter));
        context.Enqueue(new PostHubOrder(
          FirmId, loc, _policy.OutputProduct, HubOrderSide.Sell,
          Quantity.From(sellQty), Money.From(Math.Round(px, 2))));
        LastDecision = $"sell ×{sellQty:0} @ {site.Name}";
      }

      if (input < _policy.InputFloor)
      {
        var need = _policy.InputFloor - input + 4m;
        var px = _policy.InputLimitPrice * (1m + ((decimal)rng.NextDouble() * _policy.PriceJitter));
        context.Enqueue(new PostHubOrder(
          FirmId, loc, _policy.InputProduct, HubOrderSide.Buy,
          Quantity.From(need), Money.From(Math.Round(px, 2))));
        LastDecision = $"bid input ×{need:0} @ {site.Name}";
      }

      if (rate <= 0m)
      {
        LastDecision = input < _policy.InputPerOutput
          ? $"starved @ {site.Name}"
          : $"idle (cap) @ {site.Name}";
      }
    }
  }

  private decimal Qty(AgentContext context, InventoryLocationId loc, ProductId p) =>
    context.World.Inventory.GetQuantity(new InventoryKey(FirmId, loc, p)).Value;
}
