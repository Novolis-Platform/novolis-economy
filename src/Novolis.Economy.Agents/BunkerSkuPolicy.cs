using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>Bunker / energy stock policy at a site.</summary>
public sealed record BunkerSkuPolicy(
  ProductId ProductId,
  decimal MinStock,
  decimal BuyLimitPrice,
  decimal SellPrice,
  bool AllowProcurement);
