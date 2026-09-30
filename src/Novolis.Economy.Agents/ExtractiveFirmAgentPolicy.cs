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
