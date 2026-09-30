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
