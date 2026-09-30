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
