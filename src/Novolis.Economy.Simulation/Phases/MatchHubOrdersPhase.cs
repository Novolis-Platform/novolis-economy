using Novolis.Economy;
using Novolis.Economy.Agents;
using Novolis.Economy.Accounting;
using Novolis.Economy.Finance;
using Novolis.Economy.Logistics;
using Novolis.Economy.Markets;
using Novolis.Economy.Population;
using Novolis.Economy.Production;

namespace Novolis.Economy.Simulation.Phases;

public sealed class MatchHubOrdersPhase : ISimulationPhase
{
  /// <inheritdoc />
  public SimulationPhaseOrder Order => SimulationPhaseOrder.MatchHubOrders;

  /// <inheritdoc />
  public ValueTask ExecuteAsync(SimulationContext context, CancellationToken cancellationToken)
  {
    var world = context.State.World;
    var hour = context.State.Clock;
    var open = world.HubOrders.Where(o => !o.IsFilled).ToList();
    var groups = open
      .GroupBy(o => (o.LocationId, o.ProductId))
      .OrderBy(g => g.Key.LocationId.Value)
      .ThenBy(g => g.Key.ProductId.Value);

    foreach (var group in groups)
    {
      var buys = group
        .Where(o => o.Side == HubOrderSide.Buy)
        .OrderByDescending(o => o.LimitPrice.Amount)
        .ThenBy(o => o.PostedAt.HourIndex)
        .ThenBy(o => o.Id)
        .ToList();
      var sells = group
        .Where(o => o.Side == HubOrderSide.Sell)
        .OrderBy(o => o.LimitPrice.Amount)
        .ThenBy(o => o.PostedAt.HourIndex)
        .ThenBy(o => o.Id)
        .ToList();

      var bi = 0;
      var si = 0;
      while (bi < buys.Count && si < sells.Count)
      {
        var buy = buys[bi];
        var sell = sells[si];
        if (buy.FirmId.Equals(sell.FirmId))
        {
          si++;
          continue;
        }

        if (buy.LimitPrice.Amount + 0.0000001m < sell.LimitPrice.Amount)
        {
          break;
        }

        var fillQty = Math.Min(buy.Remaining.Value, sell.Remaining.Value);
        if (fillQty <= 0m)
        {
          if (buy.Remaining.Value <= 0m) bi++;
          if (sell.Remaining.Value <= 0m) si++;
          continue;
        }

        var unitPrice = sell.LimitPrice; // maker sell price (deterministic)
        var spend = Money.From(fillQty * unitPrice.Amount);
        if (!world.Ledgers.TryGetValue(buy.FirmId, out var buyerLedger)
            || !world.Ledgers.TryGetValue(sell.FirmId, out var sellerLedger)
            || buyerLedger.Cash.Amount + 0.0000001m < spend.Amount)
        {
          bi++;
          continue;
        }

        var sellerKey = new InventoryKey(sell.FirmId, sell.LocationId, sell.ProductId);
        if (!CoreInventoryBridge.TryTake(
              world,
              sellerKey,
              Quantity.From(fillQty),
              out var taken,
              out var cogs))
        {
          si++;
          continue;
        }

        var buyerKey = new InventoryKey(buy.FirmId, buy.LocationId, buy.ProductId);
        foreach (var lot in taken)
        {
          CoreInventoryBridge.Add(
            world,
            buyerKey,
            lot with { UnitCost = unitPrice });
        }

        LedgerEngine.PostCashSale(sellerLedger, spend, cogs, hour.Date);
        LedgerEngine.PostCashPurchase(buyerLedger, spend, hour.Date);

        buy.Remaining = Quantity.From(buy.Remaining.Value - fillQty);
        sell.Remaining = Quantity.From(sell.Remaining.Value - fillQty);

        context.State.AppendEvent(new HubOrderFilled(
          hour, buy.Id, sell.Id, buy.FirmId, sell.FirmId,
          buy.LocationId, buy.ProductId, Quantity.From(fillQty), unitPrice));
        context.State.AppendEvent(new GoodsSoldInterFirm(
          hour, sell.FirmId, buy.FirmId, buy.LocationId, buy.ProductId,
          Quantity.From(fillQty), unitPrice, spend));
        world.MarketBook.RecordTrade(buy.ProductId, Quantity.From(fillQty), unitPrice, hour);

        if (buy.IsFilled) bi++;
        if (sell.IsFilled) si++;
      }
    }

    world.HubOrders.RemoveAll(o => o.IsFilled);
    return ValueTask.CompletedTask;
  }
}
