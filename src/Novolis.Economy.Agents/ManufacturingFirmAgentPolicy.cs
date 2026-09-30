using Novolis.Economy;
using Novolis.Economy.Markets;
using Novolis.Economy.Production;

namespace Novolis.Economy.Agents;

/// <summary>Thresholds for a multi-product plant.</summary>
public sealed record ManufacturingFirmAgentPolicy(
  IReadOnlyList<AgentSite> Sites,
  ProductId PrimaryInput,
  decimal PrimaryInputFloor,
  decimal PrimaryInputLimitPrice,
  IReadOnlyList<ManufacturedSkuPolicy> Outputs,
  decimal PriceJitter = 0.04m);
